function [out1, out2] = calculate_cross_section(cs, slope, avg_temp, mass, drift_length, buffer_gas)
    % init variables
    proton_mass = 1.00782 ;

    %calculate K0
    K0 = ((drift_length)^2/slope) * (273.15/avg_temp) * (1/760) * (1/0.001) ;
    MH = mass + proton_mass ;
    reduced_mass = (MH * buffer_gas)/(MH + buffer_gas) ;
    omega = 18459 * sqrt(inv(reduced_mass * avg_temp)) * (cs/K0) ;

    out1 = K0 ;
    out2 = omega ;
    return
end
