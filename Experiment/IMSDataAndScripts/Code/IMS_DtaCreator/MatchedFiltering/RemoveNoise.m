function out = RemoveNoise (X_smooth, X, shift, Y)

out = zeros(size(X));
snr_Y = SignalNoiseRatio (Y);
snr_X = SignalNoiseRatio (X);

%option 3: look at every peak in the Y channel,
%Take out FWHM of that and set lboundary and rboudary
%correspondigly for selection in Xs
for i = 1:size(X,1)
    for j = 2:size(X,2)-1
        if (Y(i,j) > Y(i,j-1) && Y(i,j) > Y(i,j+1))
            if (Y(i,j) > snr_Y(i))
                set_shoulder = 0;
                c = j+1;
                width = 0;
                while (Y(i,c) > Y(i,j)/2)
                    c = c+1;
                    width = width+1;
                    if (c>size(X,2))
                        break;
                    end
                    if (Y(i,c) > Y(i,c-1))
                        set_shoulder = 1;
                    end
                end
                rboundary = j+2*width;
                if rboundary >size(X,2)
                    rboundary = size(X,2);
                end
                c = j-1;
                width = 0;
                while (Y(i,c) > Y(i,j)/2)
                    width = width +1;
                    c = c-1;
                    if (c < 1)
                        break;
                    end
                    if (Y(i,c) > Y(i,c+1))
                        set_shoulder = 1;
                    end
                end
                lboundary = j - 2*width;
                if lboundary < 1
                    lboundary = 1;
                end
                if (set_shoulder == 0)
                    array = X(i,[lboundary:rboundary]);
                    xwidth = GetZeroCrossings(array, snr_X);
                    if (xwidth>2)
                        for c = lboundary:rboundary
                            if (X(i,c) > snr_X)
                                out(i,c) = X(i,c);
                            end
                        end
                    end
                end
            end
        end
    end
end