function out = butterworth (X)
%X is a row vector

[B, A] = butter(3, 0.1);
out = abs(filtfilt(B, A, X));

