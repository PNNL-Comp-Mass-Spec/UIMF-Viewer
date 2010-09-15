%%computes a moving average in increments for period
%%mostly used to computer a 3 point moving average for data smooting
%%the filter function in Matlab produces different results than this
%%routine



function movAverage = MovingAverage(inArray, period)
    movAverage = [];
    
    for i=1:size(inArray)
        sum = 0;
        counter = 0;
        
        try
            for j = 1:period
                sum = sum + inArray(i+j-1);
                counter = counter + 1;
            end;
        catch
        end;
        
        sum = sum / counter;
        movAverage = [movAverage; sum];
        
    end;
