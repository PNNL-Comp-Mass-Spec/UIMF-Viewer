function [out1] = GetUniqueElements(vect_data) 
    % simple function to return just the unique elements  in an array
    sorted_data = sort(vect_data) ; 
    I = [2: size(sorted_data, 1)] ; 
    diff = sorted_data(I) - sorted_data(I-1) ; 
    diff = [diff ; 1];
    I2 = find(diff>0) ; 
    out1 = sorted_data(I2) ; 
    return ; 
end