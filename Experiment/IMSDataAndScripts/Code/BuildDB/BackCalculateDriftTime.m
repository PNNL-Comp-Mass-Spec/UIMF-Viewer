function out = BackCalculateDriftTime(omega, avg_temp, intercept, cs, mono_mass, buffer_gas, voltage, drift_length, slope )
% init variables
proton_mass = 1.00782 ;
std_pressure = 4 ;
if(nargin<9)
    % first back calculate mobility
    massH = mono_mass + proton_mass ;
    reduced_mass = (massH * buffer_gas)/(massH + buffer_gas) ;
    K0 = 18459 * sqrt(inv(reduced_mass * avg_temp)) * (cs/omega) ;

    %now back-caculate slope
    slope = ((drift_length)^2/K0) * (273.15/avg_temp) * (1/760) * (1/0.001) ;

    % P/v point is
    PV = std_pressure/voltage ;
    drift_time = slope*PV + intercept ;

    out = drift_time ;
end

if (nargin == 9)
    % P/v point is
    PV = std_pressure/voltage ;
    drift_time = slope*PV + intercept ;

    out = drift_time ;
end
