function out = SearchForMTIDsUsingRange(MassTagInfo, UMCInfo, mass_err_range, net_err_range, dt_err_range)

num_umcs = size(UMCInfo, 1) ;
out = [] ;
for i = 1:num_umcs
    this_umc = UMCInfo(i, 1) ;
    this_mass = UMCInfo(i, 2) ;
    this_net = UMCInfo(i, 3) ;
    this_dt  = UMCInfo(i, 4) ;
    ppm_diff = (MassTagInfo(:, 2)- this_mass)/this_mass * 1000000;
    net_diff = (MassTagInfo(:, 3) - this_net);
    dt_diff = (MassTagInfo(:, 4) - this_dt) ;
    
    error = [ppm_diff net_diff dt_diff] ; 
    I = find((error(:,1)>mass_err_range(1) & error(:,1)<mass_err_range(2)) & (error(:,2)>net_err_range(1) & error(:,2)<net_err_range(2)) & (error(:,3)>dt_err_range(1) & error(:,3)<dt_err_range(2))) ; 
    for j = 1:size(I, 1)
        row = [this_umc MassTagInfo(I(j), 1) ppm_diff(I(j)) net_diff(I(j)) dt_diff(I(j)) UMCInfo(i, 5)] ;
        out = [out; row] ;
    end
end