function out = FullWidthHalfMax(X, max_peak_index)
max_peak = X(max_peak_index) ; 
rboundary = max_peak_index ; 
i = 1 ; 
peak = X(max_peak_index-1) ; 
prev_peak = max_peak ; 
while ((peak > max_peak/2) && (peak < prev_peak) )
    prev_peak = peak ; 
    i = i+1 ; 
    peak = X(max_peak_index-i) ; 
end
lboundary = max_peak_index - i ; 

i = 1 ; 
peak = X(max_peak_index+1) ; 
prev_peak = max_peak ; 
while ((peak > max_peak/2) && (peak < prev_peak) )
    prev_peak = peak ; 
    i = i+1 ; 
    peak = X(max_peak_index+i) ; 
end
rboundary = max_peak_index + i ; 




FWHM = (rboundary - lboundary) ; 
out = FWHM ; 