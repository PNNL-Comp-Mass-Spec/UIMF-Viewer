function [peaklist] = getPeaks (umc_profiles, umc_index)
     indices = find(umc_profiles(:,12) == umc_index);
     peaklist = umc_profiles(indices,:);
     