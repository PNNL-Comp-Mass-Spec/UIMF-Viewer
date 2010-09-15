function [out1, out2] = ResolveConformers(obs_drift_times, obs_intensity_values, obs_mass_values, this_mass_tag_id, plot_fig)
%function to look at drift time profiles and detect conformers using
%model-based clustering methods  in mclust

% this procedure will be a two-pass process
%  1) sort by drift times and perform simple single linkage clustering
%     based on drift times
%  2) refine the process and use mclust giving it the number of umcs found
%      in step 1



X = [obs_drift_times, obs_intensity_values, obs_mass_values] ;
X_sorted = sortrows(X, 1) ;

X = X_sorted ; 


if plot_fig
    figure ;
    plot(X(:,1), X(:,2)) ;
    xlabel('Drift Time') ; 
    ylabel('Intensity') ; 
    title('MTID Drift Profile') ; 
    hold on; 
end

% minD = min(X_sorted(:,1)) ; 
% maxD = max(X_sorted(:,1)) ;
% x = linspace(minD, maxD, 100) ; 
% stepsize = (maxD - minD)/100 ; 
% y = zeros(100, 1)' ;
% for i = 1:length(X_sorted)
%     thisD = X_sorted(i,1) ; 
%     bin = round((thisD - minD)/stepsize) + 1 ; 
%     if (bin > 100)
%         bin = 100 ; 
%     end    
%     y(bin) = y(bin)+  X_sorted(i,2) ; 
% end


%% Trying out alternate way to choose num_clusters
sample_size = 100 ;
new_intensities = (X_sorted(:,2) * sample_size)/sum(X_sorted(:,2)) ;
X_sorted(:,2) = round(new_intensities) ;
new_intensities = X_sorted(:, 2) ;
I = find(new_intensities < quantile(new_intensities, 0.25)) ;
n = length(I) ;

if n > 0
    if (I(1) == 1) %|| I(n) == length(new_intensities))
        num_clusters_so_far = 0 ;
    else
        num_clusters_so_far = 1 ;  %% one greater than
    end
else
    num_clusters_so_far = 1 ; 
end
for i = 2:length(I)
    diff = I(i) - I(i-1) ;
    if (diff > 2) % have to be spread apart by more than one scan
        num_clusters_so_far = num_clusters_so_far + 1;
    end
end

% Ialt = find(new_intensities > quantile(new_intensities, 0.25)) ;
% newX = X_sorted(Ialt, :) ; 
% X_sorted = newX ; 

d1 = [] ;
d2 = [] ;
d = [];
for i = 1:size(X_sorted,1)
    d1 = [d1 repmat(X_sorted(i,1), 1, X_sorted(i,2))];
    d2 = [d2 repmat(X_sorted(i,3), 1, X_sorted(i, 2))] ;
end


d = [d1' d2'] ;





% % perform single-linkage clustering
% sorted_drift_times = X_sorted(:, 1) ;
% num_points = size(sorted_drift_times, 1) ;
% current_index = 1;
% num_clusters_so_far = 0 ;
% min_points_cluster = 3;
% dt_tolerance = 4 ;
%
% while(current_index <= num_points)
%     current_dt = sorted_drift_times(current_index) ;
%     match_index = current_index + 1 ;
%
%     cluster_drifts = [] ;
%     if (match_index > num_points)
%         break ;
%     end
%
%     cluster_drifts = [cluster_drifts ; current_dt] ;
%     max_drift = current_dt + dt_tolerance ;
%     match_dt = sorted_drift_times(match_index) ;
%
%     while (match_dt <= max_drift)
%         % found a guy within the same dt tolerance
%         cluster_drifts = [cluster_drifts ; match_dt] ;
%         match_index = match_index + 1 ;
%
%         if (match_index > num_points)
%              break ;
%         end
%         match_dt = sorted_drift_times(match_index) ;
%     end
%
%     if (size(cluster_drifts, 1) >= min_points_cluster)
%         num_clusters_so_far = num_clusters_so_far + 1;
%     end
%
%     current_index = match_index ;
% end


if (num_clusters_so_far > 1)
    % now call mclust
    [bics, bestmodel, allmodels, z, clabs] = mbclust(d, num_clusters_so_far) ;


    %
    num_clusters = size(bestmodel.mus, 2) ;
    out2 = bestmodel.mus(1,:) ;
    out1 = [] ;
    for i = 1: num_clusters
        id = this_mass_tag_id + i/10 ;
        out1 = [out1 id] ;
        if (plot_fig)
            y = linspace(0, max(X(:,2)), 10) ; 
            x = repmat(out2(i), 10, 1)'; 
            plot(x, y, 'r') ; 
        end       
    end
else
    [Imax index] = max(X(:,2)) ; 
    out2 = X(index,1) ;
    out1 = this_mass_tag_id ;
    if(plot_fig)
        y = linspace(0, max(X(:,2)), 10) ; 
        x = repmat(X(index,1), 10, 1)' ;
        plot(x, y, 'r') ; 
    end
end


        



return

