function out = RemoveSinglePeaks(X)
for i = 1:size(X,1)
    for j = 2:size(X,2)-1
        if (X(i,j) > 0)
            if ((X(i,j-1) == 0) && (X(i, j+1) == 0))
                X(i,j) = 0;
            end
        end
    end
end    
out = X;