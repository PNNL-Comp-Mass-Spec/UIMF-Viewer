function out = GetZeroCrossings (X, snr_X)
for i = 1:size(X,1)
    if max(X(i,:) > 0)
        j = find(X(i,:) == max(X(i,:)));
        c = j;
        while ((X(i,c) >0))
            c = c+1;
            if c > size (X,2)
                break;
            end
        end
        rboundary = c;
        c =j;
        while ((X(i,c) >0))
            c = c-1;
            if (c < 1)
                break;
            end
        end
        lboundary = c;
        out(i) = rboundary - lboundary;
    else
        out(i) = 1;
    end
end