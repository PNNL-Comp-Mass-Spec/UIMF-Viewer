function [out1, out2] = FindPeaks(X, snr) 
% X is a row vector
peak_count = 1;
peak_val = [] ; 
peak_index = [] ; 

for i = 2: size(X,2)-1
    if (X(i) > snr)
        if X(i) > X(i-1) && X(i) > X(i+1)
            peak_val(peak_count) = X(i);
            peak_index(peak_count) = i ; 
            peak_count = peak_count + 1;
        end
    end
end

out1 = peak_val ; 
out2 = peak_index ; 