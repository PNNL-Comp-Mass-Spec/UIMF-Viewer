% Function to calculate a nominal error window using EM
% algorithm. Modified from Deep's example which was adapted from Xiuxia's
% EM algorithm 1_11_05
% Input has to be a row vector of delFit values.
function [sigma_k mu_k] = EM_1D(m, plot_figs) ;


%% initial condition
N = size(m, 2);
pi_m = 0.5;    %% weight of the normal distribution
mu_m = 1;      %% mean of the normal distribution
var_m = 1.5;   %% variance of the mass

m_max = max(m);
m_min = min(m);
u = 1 / (m_max-m_min);

NumIteration = 30;
pi_All = zeros(1,NumIteration+1); %These three lines create a m x n matrix of zeros
mu_All = zeros(1,NumIteration+1);
var_All = zeros(1,NumIteration+1);

pi_All(1) = pi_m; %%Places the initial probability condition as the first place holder in pi_All
mu_All(1) = mu_m; %Places the initial mean in the first column of the mu_All matrix
var_All(1) = var_m;

%% Anoop: commmented out this plot
% f1 = figure;
% hold on ;
% 
% clr_hot = hot ;

for k = 1:NumIteration

    zi = zeros(1,N); % Creates a matrix from the sample size

    %% expectation step
    for i = 1:N(1)        
        xi = m(1, i);
        prob_normal = (1/(sqrt(2 * pi) * sqrt(var_m))) * exp(-1/2 * ((xi - mu_m)^2)/(var_m)); %* (xi - mu_m));
        zi(i) = (1 - pi_m) * u / (pi_m * prob_normal + (1 - pi_m) * u);
    end
    
    %% Anoop: commmented out this plot
%     figure(f1) ;
%     clr = clr_hot(k,:) ;
%     plot(zi,'.', 'MarkerEdgeColor', clr,'MarkerFaceColor', clr)
%     xlabel('Peptide')
%     ylabel('Expectation')
%     title('Expectation with Each Iteration')

    %% maximization step
    pi_m_num = 0;
    mu_m_num = 0;
    var_m_num = 0;
    for i = 1:N
        xi = m(1, i);

        pi_m_num = pi_m_num + (1 - zi(i));

        mu_m_num = mu_m_num + (1 - zi(i)) * xi;

        var_m_num = var_m_num + (1 - zi(i)) * (xi - mu_m)^2;
    end
    pi_m = pi_m_num /N(1);
    mu_m = mu_m_num / pi_m_num;

    var_m = var_m_num / pi_m_num;

    pi_All(1,k+1) = pi_m;
    mu_All(1,k+1) = mu_m;
    var_All(1,k+1) = var_m;
    
    sigma_k = sqrt(var_m) ; % Anoop 
    mu_k = mu_m ;  %Anoop

end

if (plot_figs == 1)
    figure; plot(pi_All,'.')
    xlabel('Iteration')
    ylabel('Probability (\pi)')
    title('Probability with Iteration');
    figure; plot(mu_All,'.')
    xlabel('Iteration')
    ylabel('Distribution Mean (\mu)')
    title('Mean with Iteration');
    figure; plot(var_All, '.')
    xlabel('Iteration')
    ylabel('Variance (\sigma^2)')
    title('Variance with Iteration');
end

