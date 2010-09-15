function out = GetIntensity(Y, X)
%Get peak in X closest to maximum of Y and return intensity
out = 0 ;
snr_Y = SignalNoiseRatio (Y);
snr_X = SignalNoiseRatio (X);

% [peaks_X_val, peaks_X_index] = FindPeaks(X, 4 * snr_X) ; 
% [peaks_Y_val, peaks_Y_index] = FindPeaks(Y, snr_Y) ; 
% 
% max_peak_y = max(peaks_Y_val) ; 
% max_peak_y_index = find(Y == max_peak_y) ; 
% 
% 
% min_diff = 20 ; 
% index = 0 ; 
% 
% for i = 1:size(peaks_X_val, 2) 
%     diff = abs(peaks_X_index(i)- max_peak_y_index) ;
%     if (diff  < min_diff )
%         min_diff = diff ; 
%         index = i ; 
%     end
% end
% 
% if (index == 0 )
%     out = 0 ;
% else
%     out = peaks_X_val(index);
% end
   
%Sum all intensities
max_y = max(Y) ;
max_y_index = find(Y == max_y) ;

%find fwhm 
width = 0 ;
i = max_y_index ; 
while (Y(i) > max_y/2)
    width = width+1;
    if (i>size(Y,2))
        break;
    end    
    i = i +1 ; 
end
rboundary = max_y_index+2*width;
if rboundary >size(Y,2)
    rboundary = size(X,2);
end


width = 0 ;
i = max_y_index ; 
while (Y(i) > max_y/2)
    width = width+1;
    if (i < 1)
        break;
    end    
    i = i -1 ; 
end
lboundary = max_y_index - 2*width;
if lboundary < 1
    rboundary = 1 ; 
end

intensity = sum(X(lboundary:rboundary)) ; 
out = intensity ; 
