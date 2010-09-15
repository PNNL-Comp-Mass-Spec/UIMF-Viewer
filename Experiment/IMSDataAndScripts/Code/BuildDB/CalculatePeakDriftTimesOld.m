function [out_resolved_mass_tags, out_peak_drift_times]=CalculatePeakDriftTimesOld(obs_drift_times, obs_intensity_values, obs_mass_tag_id)
% function to look at drift_time profile and determine peaks

% % peak_indices = [] ;
% % I = [] ;
% % n = size(obs_drift_times, 1) ;
% % %
% % % sort drift times first
% sorted_drift_times = sort(obs_drift_times) ;
% for i = 1:n
%     I = [I ; find(obs_drift_times == sorted_drift_times(i))] ;
% end
% 
% % get peaks out from drift profile
% peak_intensities = obs_intensity_values(I) ;
% % for i = 2:n-1
% %     if ((peak_intensities(i) > peak_intensities(i-1)) & (peak_intensities(i) > peak_intensities(i+1)))
% %         peak_indices = [peak_indices, i] ;
% %     end
% % end
% % num_peaks = size(peak_indices, 1) ;
% % 
% % 
% %  if (obs_mass_tag_id == 332)
% %      figure ;
% %      plot(sorted_drift_times, peak_intensities) ;
% %      keyboard ;
% %  end

%Ignore conformers for the time being
out_resolved_mass_tags = obs_mass_tag_id ;

%out_peak_drift_times = mean(sorted_drift_times(peak_indices)) ;
out_peak_drift_times = mean(obs_drift_times) ;

%indx = find ( obs_intensity_values == max(obs_intensity_values));
%out_peak_drift_times = obs_drift_times(indx);

%%Calculate the weighted average
%products = obs_drift_times .* obs_intensity_values;

%out_peak_drift_times = sum(products) / sum(obs_intensity_values);

end