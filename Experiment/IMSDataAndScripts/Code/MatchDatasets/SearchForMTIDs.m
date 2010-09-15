%% Function to search using given search tolerances
function out = SearchForMTIDs(MassTagInfo, UMCInfo, mass_tol, net_tol, dt_tol)
num_umcs = size(UMCInfo, 1) ;
out = [] ;
test = [] ; 
for i = 1: num_umcs
    this_umc = UMCInfo(i, 1) ;
    this_mass = UMCInfo(i, 2) ;
    this_net = UMCInfo(i, 3) ;
    this_dt  = UMCInfo(i, 4) ;
    ppm_diff = (MassTagInfo(:, 2)- this_mass)* 1000000/this_mass ;
    net_diff = (MassTagInfo(:, 3) - this_net);
    dt_diff = (MassTagInfo(:, 4) - this_dt) ;
    error = [abs(ppm_diff) abs(net_diff) abs(dt_diff)] ;
    I = find(error(:, 1) < mass_tol & error(:,2) < net_tol & error(:,3) < dt_tol) ;
    for j = 1: size(I, 1)
        row = [this_umc MassTagInfo(I(j), 1) ppm_diff(I(j)) net_diff(I(j)) dt_diff(I(j)) UMCInfo(i, 5)] ;
        out = [out; row] ;
    end
end


% You want to rewrite the above without using loops
%
% 
% 
%
%
%
%
%
%
%
%
%
%
%
%

end