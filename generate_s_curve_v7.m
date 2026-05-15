function [t, x, v, a, j] = generate_s_curve_v7(x0, xg, v0, vg, v_max, a_max, j_max, dt, plot_flag)
% 7段S型轨迹规划 (v7 - 优化版)
%
% 输入参数:
%   x0       - 起始位置
%   xg       - 目标位置
%   v0       - 初始速度
%   vg       - 终止速度
%   v_max    - 最大速度 (>0)
%   a_max    - 最大加速度 (>0)
%   j_max    - 最大加加速度 (>0)
%   dt       - 采样时间步长 (默认 0.001s)
%   plot_flag- 是否绘图 (默认 false)
%
% 输出参数:
%   t - 时间向量
%   x - 位置向量
%   v - 速度向量
%   a - 加速度向量
%   j - 加加速度向量
%
% 优化要点:
%   [FIX-1] 修正二分法位移公式，改用物理一致的链式状态积分
%   [FIX-2] 段7解析式与链式状态对齐，消除终端跳变
%   [FIX-3] 负方向运动时v0_/vg_符号处理统一，避免速度方向错误
%   [FIX-4] 增加短程无匀速段时的残余误差补偿（拉伸匀速段）
%   [OPT-1] calc_params 提取为独立函数，增加 dv<0 保护
%   [OPT-2] 向量化分段计算，统一 eps_b 保护
%   [OPT-3] 增加参数校验与更友好的错误提示
%   [OPT-4] plot_flag 支持子图标题与段边界标注

    %% 0. 默认参数
    if nargin < 8 || isempty(dt),        dt        = 1e-3;  end
    if nargin < 9 || isempty(plot_flag), plot_flag = false;  end

    %% 1. 输入校验
    validateattributes(v_max,  {'numeric'}, {'scalar','positive'}, mfilename, 'v_max');
    validateattributes(a_max,  {'numeric'}, {'scalar','positive'}, mfilename, 'a_max');
    validateattributes(j_max,  {'numeric'}, {'scalar','positive'}, mfilename, 'j_max');
    validateattributes(dt,     {'numeric'}, {'scalar','positive'}, mfilename, 'dt');

    %% 2. 零位移快速返回
    if abs(xg - x0) < 1e-12
        t = 0; x = x0; v = v0; a = 0; j = 0;
        return;
    end

    %% 3. 方向归一化（所有规划在正方向下完成）
    dir  = sign(xg - x0);
    S    = abs(xg - x0);       % 总位移量（正）
    v0_  = v0 * dir;           % 归一化初速度
    vg_  = vg * dir;           % 归一化末速度

    % [FIX-3] 钳位：确保归一化速度不超过 v_max，且非负
    v0_  = clamp(v0_,  0, v_max);
    vg_  = clamp(vg_,  0, v_max);

    %% 4. 加减速段参数计算
    [Ta, Tj1, Ap] = scurve_segment(v0_, v_max, a_max, j_max);  % 加速段
    [Td, Tj2, Dp] = scurve_segment(vg_, v_max, a_max, j_max);  % 减速段

    % 加减速段各自的位移（链式面积公式，精确）
    sa = 0.5 * (v0_ + v_max) * Ta;
    sd = 0.5 * (vg_ + v_max) * Td;

    %% 5. 确定峰值速度 v_lim 与匀速段时间 Tv
    if S >= sa + sd
        % 情况A：可到达 v_max
        v_lim = v_max;
        Tv    = (S - sa - sd) / v_max;
    else
        % 情况B：短程，二分法求 v_lim < v_max
        % [FIX-1] 用链式面积公式（与sa/sd一致）而非报告公式
        v_lim = bisect_vlim(v0_, vg_, S, a_max, j_max);
        [Ta, Tj1, Ap] = scurve_segment(v0_, v_lim, a_max, j_max);
        [Td, Tj2, Dp] = scurve_segment(vg_, v_lim, a_max, j_max);
        sa = 0.5 * (v0_ + v_lim) * Ta;
        sd = 0.5 * (vg_ + v_lim) * Td;
        Tv = 0;

        % [FIX-4] 残余位移补偿：将误差并入匀速段（误差通常 < 1e-9）
        residual = S - sa - sd;
        if residual > 1e-9 && v_lim > 1e-12
            Tv = residual / v_lim;
        elseif residual < -1e-9
            warning('generate_s_curve_v7:displacement', ...
                '短程位移残差 %.2e，请检查输入参数', residual);
        end
    end

    %% 6. 各段时间节点
    T = Ta + Tv + Td;   % 总时间

    % dt 过大警告
    min_seg = min(nonzeros([Tj1, Ta-Tj1, Tj2, Td-Tj2]));
    if ~isempty(min_seg) && dt > min_seg / 3
        warning('generate_s_curve_v7:timestep', ...
            'dt(%.4f) 可能过大，最小段时长 %.4f，建议 dt < %.4f', ...
            dt, min_seg, min_seg/3);
    end

    % 7个时间节点（段边界）
    tm    = zeros(1,7);
    tm(1) = Tj1;
    tm(2) = Ta - Tj1;    % [OPT-2] 直接存储相对于段起点的累计时间...
    tm(2) = max(tm(1), Ta - Tj1);  % 修正为全局时刻
    tm(3) = Ta;
    tm(4) = Ta + Tv;
    tm(5) = Ta + Tv + Tj2;
    tm(6) = max(tm(5), T - Tj2);
    tm(7) = T;

    %% 7. 链式节点状态（消除跳变的关键）
    % 节点1：加加速结束
    v1 = v0_ + 0.5*j_max*Tj1^2;
    x1 = v0_*Tj1 + j_max*Tj1^3/6;

    % 节点2：匀加速结束（或与节点1重合当Ta=2*Tj1时）
    dt21 = tm(2) - tm(1);
    v2   = v1 + Ap * dt21;
    x2   = x1 + v1*dt21 + 0.5*Ap*dt21^2;

    % 节点3：加速段结束（v = v_lim）
    dt32 = tm(3) - tm(2);
    v3   = v2 + Ap*dt32 - 0.5*j_max*dt32^2;
    x3   = x2 + v2*dt32 + 0.5*Ap*dt32^2 - j_max*dt32^3/6;

    % 节点4：匀速段结束
    v4   = v3;
    x4   = x3 + v3*(tm(4)-tm(3));

    % 节点5：减加加速（开始减速）结束
    dt54 = tm(5) - tm(4);
    v5   = v4 - 0.5*j_max*dt54^2;
    x5   = x4 + v4*dt54 - j_max*dt54^3/6;

    % 节点6：匀减速结束
    dt65 = tm(6) - tm(5);
    v6   = v5 - Dp*dt65;      %#ok<NASGU> % 备用校验
    x6   = x5 + v5*dt65 - 0.5*Dp*dt65^2;

    % 节点7（终点）由段7解析式精确锚定（见下文FIX-2）

    %% 8. 时间向量生成
    if T < dt
        t = [0; T];
    else
        t = (0 : dt : T)';
        if T - t(end) > dt/2
            t(end+1) = T;   % 末点不够近，追加终点
        else
            t(end) = T;     % 末点已很近，就地修正为精确终点
        end
    end
    N = numel(t);

    %% 9. 向量化分段计算
    xr = zeros(N,1);
    vr = zeros(N,1);
    ar = zeros(N,1);
    jr = zeros(N,1);
    eb = 1e-12;   % 边界容差

    % 段1：加加速 [0, tm1]
    m = t <= tm(1) + eb;
    tau = t(m);
    xr(m) = v0_*tau + j_max*tau.^3/6;
    vr(m) = v0_ + 0.5*j_max*tau.^2;
    ar(m) = j_max*tau;
    jr(m) = j_max;

    % 段2：匀加速 (tm1, tm2]
    m = t > tm(1)+eb & t <= tm(2)+eb;
    tau = t(m) - tm(1);
    xr(m) = x1 + v1*tau + 0.5*Ap*tau.^2;
    vr(m) = v1 + Ap*tau;
    ar(m) = Ap;
    jr(m) = 0;

    % 段3：减加加速 (tm2, tm3]
    m = t > tm(2)+eb & t <= tm(3)+eb;
    tau = t(m) - tm(2);
    xr(m) = x2 + v2*tau + 0.5*Ap*tau.^2 - j_max*tau.^3/6;
    vr(m) = v2 + Ap*tau - 0.5*j_max*tau.^2;
    ar(m) = Ap - j_max*tau;
    jr(m) = -j_max;

    % 段4：匀速 (tm3, tm4]
    m = t > tm(3)+eb & t <= tm(4)+eb;
    tau = t(m) - tm(3);
    xr(m) = x3 + v3*tau;
    vr(m) = v3;
    ar(m) = 0;
    jr(m) = 0;

    % 段5：加负加加速 (tm4, tm5]
    m = t > tm(4)+eb & t <= tm(5)+eb;
    tau = t(m) - tm(4);
    xr(m) = x4 + v4*tau - j_max*tau.^3/6;
    vr(m) = v4 - 0.5*j_max*tau.^2;
    ar(m) = -j_max*tau;
    jr(m) = -j_max;

    % 段6：匀减速 (tm5, tm6]
    m = t > tm(5)+eb & t <= tm(6)+eb;
    tau = t(m) - tm(5);
    xr(m) = x5 + v5*tau - 0.5*Dp*tau.^2;
    vr(m) = v5 - Dp*tau;
    ar(m) = -Dp;
    jr(m) = 0;

    % 段7：减负加加速 (tm6, T] — [FIX-2] 从终点反向锚定
    m = t > tm(6)+eb;
    tau = T - t(m);   % 距终点的剩余时间
    xr(m) = S - (vg_*tau + j_max*tau.^3/6);
    vr(m) = vg_ + 0.5*j_max*tau.^2;
    ar(m) = -j_max*tau;
    jr(m) = j_max;

    %% 10. 方向还原 & 终点强制钳位
    x = dir * xr + x0;
    v = dir * vr;
    a = dir * ar;
    j = dir * jr;

    % 终点精确钳位（消除最后一个采样点的数值漂移）
    x(end) = xg;
    v(end) = vg;
    a(end) = 0;
    j(end) = 0;

    %% 11. 可选绘图
    if plot_flag
        figure('Name', '7-Segment S-Curve (v7)', 'Color', 'w', ...
               'Position', [100 100 900 600]);
        labels = {'Position (m)', 'Velocity (m/s)', 'Accel (m/s²)', 'Jerk (m/s³)'};
        data   = {x, v, a, j};
        colors = {'#0072BD', '#D95319', '#77AC30', '#7E2F8E'};
        for k = 1:4
            ax = subplot(4,1,k);
            plot(t, data{k}, 'Color', colors{k}, 'LineWidth', 1.5); hold on;
            % 段边界虚线
            for b = tm
                xline(b, '--', 'Color', [0.5 0.5 0.5], 'Alpha', 0.4);
            end
            ylabel(labels{k}); grid on; ax.GridAlpha = 0.3;
            if k == 1, title('7-Segment S-Curve Trajectory (v7)'); end
            if k == 4, xlabel('Time (s)'); end
        end
    end
end

%% =========== 辅助函数 ===========

function y = clamp(x, lo, hi)
% 值域钳位
    y = max(lo, min(hi, x));
end

function [T_seg, T_j, a_pk] = scurve_segment(vs, ve, a_max, j_max)
% 计算单段(加速or减速)的时间参数
% vs, ve 均为非负，且 ve >= vs（只计算"加速到"的半段）
% 对于减速段，调用前取 vs=vg_, ve=v_lim 即可（对称性）
    dv = abs(ve - vs);
    if dv < 1e-14
        T_seg = 0; T_j = 0; a_pk = 0;
        return;
    end
    if dv * j_max <= a_max^2
        % 三角形加速度剖面（无匀加速段）
        T_j   = sqrt(dv / j_max);
        T_seg = 2 * T_j;
        a_pk  = j_max * T_j;
    else
        % 梯形加速度剖面（有匀加速段）
        T_j   = a_max / j_max;
        T_seg = T_j + dv / a_max;
        a_pk  = a_max;
    end
end

function v_lim = bisect_vlim(v0_, vg_, S, a_max, j_max)
% 二分法求短程峰值速度 v_lim
% 不变式：f(v_low)<=S, f(v_high)>S
    v_low  = max(v0_, vg_);
    v_high = sqrt(v0_^2/2 + vg_^2/2 + a_max*S);   % 物理上界估计

    % 确保 v_high 真正满足 f(v_high) >= S（至多几次上推）
    for k = 1:10
        [ta,~,~] = scurve_segment(v0_, v_high, a_max, j_max);
        [td,~,~] = scurve_segment(vg_, v_high, a_max, j_max);
        if 0.5*(v0_+v_high)*ta + 0.5*(vg_+v_high)*td >= S
            break;
        end
        v_high = v_high * 2 + 0.1;
    end
    if k == 10 && 0.5*(v0_+v_high)*ta + 0.5*(vg_+v_high)*td < S
        error('generate_s_curve:BisectionFail', ...
            '无法找到满足位移约束的峰值速度，请检查输入参数是否合法（如 a_max, j_max 是否过小）');
    end

    for ~ = 1:60    % 60次迭代 → 精度约 2^-60 * v_range
        v_mid = 0.5 * (v_high + v_low);
        [ta,~,~] = scurve_segment(v0_, v_mid, a_max, j_max);
        [td,~,~] = scurve_segment(vg_, v_mid, a_max, j_max);
        sa = 0.5*(v0_+v_mid)*ta;
        sd = 0.5*(vg_+v_mid)*td;
        if sa + sd > S
            v_high = v_mid;
        else
            v_low  = v_mid;
        end
    end
    v_lim = v_low;
end
