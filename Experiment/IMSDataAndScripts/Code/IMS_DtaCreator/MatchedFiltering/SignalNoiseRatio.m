function out = SignalNoiseRatio (X)
% Returns a row vector where each entry is the SNR of each row of the input
% matrix X
for i = 1:size(X,1)
    m = mean(X(i,:));
    stdev = std (X(i,:));
    I = find(X(i,:) <= (m+ 5*stdev));
    for c = 1:size(I,2)
          Y(c) = X(i,I(c)) ;
    end
    if size(I,2) ==0
        out(i) = 0;
    else
        out(i) = mean(Y);
    end
end


