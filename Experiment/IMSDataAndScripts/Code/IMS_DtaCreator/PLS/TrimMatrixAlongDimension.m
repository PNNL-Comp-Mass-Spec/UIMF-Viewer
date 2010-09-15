function [Xnew, minNew, maxNew] = TrimMatrixAlongDimension(X, dimension, min, max, delta)

x_dim = size(X);

if (dimension == 1)
    for row = 1:x_dim(1) 
        x_vector  = X(row, :) ;
        I = find(x_vector > 0 ) ;
        if (size(I) > 0)
            start_row = row ;
            break ;
        end
    end    
    for row = x_dim(1):-1:1
        x_vector  = X(row, :) ;
        I = find(x_vector > 0 ) ;
        if (size(I) > 0)
            end_row = row ;
            break ;
        end
    end
    Xnew = X([start_row:end_row], :) ; 
    minNew = min + ((start_row -1) * delta) ; 
    maxNew = max - ((x_dim(1) - end_row) * delta) ; 
    return ;
end

if (dimension == 2)
    for col = 1 : x_dim(2)
        x_vector  = X(:, col) ;
        I = find(x_vector > 0 ) ;
         if (size(I) > 0)
             start_col = col ;
             break ;
         end
    end
    
     for col = x_dim(2):-1:1
            x_vector  = X(:, col) ;
            I = find(x_vector > 0 ) ;
            if (size(I) > 0)
                end_col = col ;
                break ;
            end
        end

    % change minmz/maxmz appropriately
    Xnew = X(:, [start_col:end_col]) ; 
    minNew = min + ((start_col-1) * delta) ;
    maxNew = max - ((x_dim(2)-end_col) * delta) ;
    return ;
end 