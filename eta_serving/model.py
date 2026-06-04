import torch
import torch.nn as nn
import torch.nn.functional as F
from torch.autograd import Variable

def normalize(data, mean, std):
    return (data - mean) / std

def unnormalize(data, mean, std):
    return data * std + mean

def get_local_seq(full_seq, kernel_size, mean, std, device=torch.device('cpu')):
    seq_len = full_seq.size()[1]
    indices = torch.LongTensor(seq_len).to(device)
    torch.arange(0, seq_len, out=indices)
    indices = Variable(indices, requires_grad=False)
    first_seq = torch.index_select(full_seq, dim=1, index=indices[kernel_size - 1:])
    second_seq = torch.index_select(full_seq, dim=1, index=indices[:-kernel_size + 1])
    local_seq = first_seq - second_seq
    local_seq = (local_seq - mean) / std
    return local_seq


class Attr(nn.Module):
    def __init__(self, embed_dims, data_feature):
        super(Attr, self).__init__()
        self.embed_dims = embed_dims
        self.data_feature = data_feature

        for name, dim_in, dim_out in self.embed_dims:
            self.add_module(name + '_em', nn.Embedding(dim_in, dim_out))

    def out_size(self):
        sz = 0
        for _, _, dim_out in self.embed_dims:
            sz += dim_out
        # append total distance
        return sz + 1

    def forward(self, batch):
        em_list = []
        for name, _, _ in self.embed_dims:
            embed = getattr(self, name + '_em')
            attr_t = batch[name]
            attr_t = torch.squeeze(embed(attr_t))
            em_list.append(attr_t)

        dist_mean, dist_std = self.data_feature["dist_mean"], self.data_feature["dist_std"]
        dist = normalize(batch["dist"], dist_mean, dist_std)
        dist = normalize(dist, dist_mean, dist_std)
        em_list.append(dist)

        return torch.cat(em_list, dim=1)


class GeoConv(nn.Module):
    def __init__(self, kernel_size, num_filter, data_feature={}, device=torch.device('cpu')):
        super(GeoConv, self).__init__()
        self.kernel_size = kernel_size
        self.num_filter = num_filter
        self.data_feature = data_feature
        self.device = device

        self.state_em = nn.Embedding(2, 2)
        # 7 inputs: absolute longi, absolute lati, state embeddings (2), longi delta, lati delta, distance delta
        self.process_coords = nn.Linear(7, 16)
        self.conv = nn.Conv1d(16, self.num_filter, self.kernel_size)

    def forward(self, batch):
        longi_mean, longi_std = self.data_feature["longi_mean"], self.data_feature["longi_std"]
        current_longi = normalize(batch["current_longi"], longi_mean, longi_std)
        
        longi_diff = batch["current_longi"][:, 1:] - batch["current_longi"][:, :-1]
        longi_diff = F.pad(longi_diff, (1, 0), value=0.0)
        longi_diff = normalize(longi_diff, 0.0, longi_std)
        
        lati_mean, lati_std = self.data_feature["lati_mean"], self.data_feature["lati_std"]
        current_lati = normalize(batch["current_lati"], lati_mean, lati_std)
        
        lati_diff = batch["current_lati"][:, 1:] - batch["current_lati"][:, :-1]
        lati_diff = F.pad(lati_diff, (1, 0), value=0.0)
        lati_diff = normalize(lati_diff, 0.0, lati_std)
        
        dist_gap_mean, dist_gap_std = self.data_feature["dist_gap_mean"], self.data_feature["dist_gap_std"]
        current_dis = normalize(batch["current_dis"], dist_gap_mean, dist_gap_std)
        
        dis_diff = batch["current_dis"][:, 1:] - batch["current_dis"][:, :-1]
        dis_diff = F.pad(dis_diff, (1, 0), value=0.0)
        dis_diff = normalize(dis_diff, dist_gap_mean, dist_gap_std)

        lngs = torch.unsqueeze(current_longi, dim=2)
        lats = torch.unsqueeze(current_lati, dim=2)
        lngs_diff = torch.unsqueeze(longi_diff, dim=2)
        lats_diff = torch.unsqueeze(lati_diff, dim=2)
        diss_diff = torch.unsqueeze(dis_diff, dim=2)

        states = self.state_em(batch['current_state'].long())

        locs = torch.cat((lngs, lats, states, lngs_diff, lats_diff, diss_diff), dim=2)
        locs = torch.tanh(self.process_coords(locs))
        locs = locs.permute(0, 2, 1)

        conv_locs = F.elu(self.conv(locs)).permute(0, 2, 1)

        local_dist = get_local_seq(current_dis, self.kernel_size, dist_gap_mean, dist_gap_std, self.device)
        local_dist = torch.unsqueeze(local_dist, dim=2)

        conv_locs = torch.cat((conv_locs, local_dist), dim=2)
        return conv_locs


class SpatioTemporal(nn.Module):
    def __init__(self, attr_size, kernel_size=3, num_filter=32, pooling_method='attention',
                 rnn_type='LSTM', rnn_num_layers=1, hidden_size=128,
                 data_feature={}, device=torch.device('cpu')):
        super(SpatioTemporal, self).__init__()
        self.kernel_size = kernel_size
        self.num_filter = num_filter
        self.pooling_method = pooling_method
        self.hidden_size = hidden_size
        self.data_feature = data_feature
        self.device = device

        self.geo_conv = GeoConv(
            kernel_size=kernel_size,
            num_filter=num_filter,
            data_feature=data_feature,
            device=device,
        )
        
        if rnn_type.upper() == 'LSTM':
            self.rnn = nn.LSTM(
                input_size=num_filter + 1 + attr_size,
                hidden_size=hidden_size // 2,
                num_layers=rnn_num_layers,
                batch_first=True,
                bidirectional=True
            )
        elif rnn_type.upper() == 'RNN':
            self.rnn = nn.RNN(
                input_size=num_filter + 1 + attr_size,
                hidden_size=hidden_size // 2,
                num_layers=rnn_num_layers,
                batch_first=True,
                bidirectional=True
            )
        else:
            raise ValueError('invalid rnn_type, please select `RNN` or `LSTM`')

        self.transformer_layer = nn.TransformerEncoderLayer(
            d_model=hidden_size,
            nhead=4,
            dim_feedforward=hidden_size * 2,
            dropout=0.2,
            activation='gelu'
        )
        self.transformer = nn.TransformerEncoder(self.transformer_layer, num_layers=1)

        if pooling_method == 'attention':
            self.attr2atten = nn.Linear(attr_size, hidden_size)

    def out_size(self):
        return self.hidden_size

    def mean_pooling(self, hiddens, lens):
        hiddens = torch.sum(hiddens, dim=1, keepdim=False)
        lens = torch.FloatTensor(lens).to(self.device)
        lens = Variable(torch.unsqueeze(lens, dim=1), requires_grad=False)
        hiddens = hiddens / lens
        return hiddens

    def atten_pooling(self, hiddens, attr_t, lens):
        query = torch.tanh(self.attr2atten(attr_t))
        scores = torch.bmm(query, hiddens.transpose(1, 2)) / (self.hidden_size ** 0.5)
        
        batch_size, seq_len, _ = hiddens.shape
        mask = torch.zeros(batch_size, 1, seq_len, dtype=torch.bool, device=hiddens.device)
        for i, l in enumerate(lens):
            mask[i, 0, l:] = True
            
        scores = scores.masked_fill(mask, -1e9)
        alpha = F.softmax(scores, dim=-1)
        hiddens = torch.bmm(alpha, hiddens)
        hiddens = torch.squeeze(hiddens, dim=1)
        return hiddens

    def forward(self, batch, attr_t):
        conv_locs = self.geo_conv(batch)

        attr_t = torch.unsqueeze(attr_t, dim=1)
        expand_attr_t = attr_t.expand(conv_locs.size()[:2] + (attr_t.size()[-1], ))
        conv_locs = torch.cat((conv_locs, expand_attr_t), dim=2)

        lens = [batch["current_longi"].shape[1]] * batch["current_longi"].shape[0]
        lens = list(map(lambda x: x - self.kernel_size + 1, lens))

        packed_inputs = nn.utils.rnn.pack_padded_sequence(conv_locs, lens, batch_first=True)
        packed_hiddens, _ = self.rnn(packed_inputs)
        hiddens, lens = nn.utils.rnn.pad_packed_sequence(packed_hiddens, batch_first=True)

        batch_size, seq_len, _ = hiddens.shape
        mask = torch.zeros(batch_size, seq_len, dtype=torch.bool, device=hiddens.device)
        for i, l in enumerate(lens):
            mask[i, l:] = True

        x = hiddens.permute(1, 0, 2)
        transformer_out = self.transformer(x, src_key_padding_mask=mask)
        hiddens = transformer_out.permute(1, 0, 2)

        packed_hiddens = nn.utils.rnn.pack_padded_sequence(hiddens, lens, batch_first=True)

        if self.pooling_method == 'mean':
            return packed_hiddens, lens, self.mean_pooling(hiddens, lens)
        else:
            return packed_hiddens, lens, self.atten_pooling(hiddens, attr_t, lens)


class EntireEstimator(nn.Module):
    def __init__(self, input_size, num_final_fcs, hidden_size=128):
        super(EntireEstimator, self).__init__()
        self.input2hid = nn.Linear(input_size, hidden_size)

        self.residuals = nn.ModuleList()
        for i in range(num_final_fcs):
            self.residuals.append(nn.Linear(hidden_size, hidden_size))

        self.hid2out = nn.Linear(hidden_size, 1)

    def forward(self, attr_t, sptm_t):
        inputs = torch.cat((attr_t, sptm_t), dim=1)
        hidden = F.gelu(self.input2hid(inputs))

        for i in range(len(self.residuals)):
            residual = F.gelu(self.residuals[i](hidden))
            hidden = hidden + residual

        out = self.hid2out(hidden)
        return out


class LocalEstimator(nn.Module):
    def __init__(self, input_size, eps=10):
        super(LocalEstimator, self).__init__()
        self.input2hid = nn.Linear(input_size, 64)
        self.hid2hid = nn.Linear(64, 32)
        self.hid2out = nn.Linear(32, 1)
        self.eps = eps

    def forward(self, sptm_s):
        if isinstance(sptm_s, nn.utils.rnn.PackedSequence):
            sptm_s = sptm_s.data
        hidden = F.gelu(self.input2hid(sptm_s))
        hidden = F.gelu(self.hid2hid(hidden))
        out = self.hid2out(hidden)
        return out


class DeepTTETransformer(nn.Module):
    def __init__(self, attr_size=28, kernel_size=3, num_filter=32, pooling_method='attention',
                 rnn_type='LSTM', rnn_num_layers=1, hidden_size=128, num_final_fcs=3, final_fc_size=128,
                 alpha=0.3, eps=10, data_feature={}, device=torch.device('cpu')):
        super(DeepTTETransformer, self).__init__()
        self.data_feature = data_feature
        self.device = device

        uid_emb_size = 16
        weekid_emb_size = 3
        timdid_emb_size = 8
        uid_size = data_feature.get("uid_size", 24000)
        embed_dims = [
            ('uid', uid_size, uid_emb_size),
            ('weekid', 7, weekid_emb_size),
            ('timeid', 1440, timdid_emb_size),
        ]

        self.kernel_size = kernel_size
        self.alpha = alpha
        self.eps = eps

        self.attr_net = Attr(embed_dims, data_feature)

        self.spatio_temporal = SpatioTemporal(
            attr_size=self.attr_net.out_size(),
            kernel_size=kernel_size,
            num_filter=num_filter,
            pooling_method=pooling_method,
            rnn_type=rnn_type,
            rnn_num_layers=rnn_num_layers,
            hidden_size=hidden_size,
            data_feature=data_feature,
            device=device,
        )

        self.entire_estimate = EntireEstimator(
            input_size=self.spatio_temporal.out_size() + self.attr_net.out_size(),
            num_final_fcs=num_final_fcs,
            hidden_size=final_fc_size,
        )

        self.local_estimate = LocalEstimator(
            input_size=self.spatio_temporal.out_size(),
            eps=eps,
        )

    def forward(self, batch):
        attr_t = self.attr_net(batch)
        sptm_s, sptm_l, sptm_t = self.spatio_temporal(batch, attr_t)
        entire_out = self.entire_estimate(attr_t, sptm_t)
        
        if self.training:
            local_out = self.local_estimate(sptm_s)
            return entire_out, (local_out, sptm_l)
        else:
            return entire_out

    def predict(self, batch):
        time_mean, time_std = self.data_feature["time_mean"], self.data_feature["time_std"]
        entire_out = self.forward(batch)
        entire_out = unnormalize(entire_out, time_mean, time_std)
        return entire_out
