% Program to  build a AMT-like DB purely from drifts files and not from
% VIPER AMT identifications.
% The individual drifts files from Direct Infusion are given as input.
% Output is in the form of xls document

%**************************************************************************
%*************************************************************************

%% Initialize
% clc ;
% clear all


read = 1;

if read == 1

    clear all ;
    clc ;
    ppm_tolerance = 20 ;
    standard_pressure = 4 ;


    avg_temp = 294.1 ;
    drift_tube_length = 98 ;
%     buffer_gas = 4.0026 ; % For He
    buffer_gas = 28.01348 ; % For N2
    


    %% column numbers in input _drifts.csv file
    ims_scan_column = 2 ;
    drift_time_column  = 3 ;
    lc_scan_column = 1 ;
    mono_mass_column = 8 ;
    fit_column = 9 ;
    cs_column = 5 ;
    abundance_column = 6 ;
    pressure_column = 11  ;

    %% data structures
    all_drifts = [] ;
    output = [] ;


    current_dir = pwd ;

    %% Enter num_files
    num_pv_points =input ('Number of P/V points to consider: ') ;
    min_num_pv_points = input('Minimum number of P/V points to consider: ') ; 
    min_R2_value = input('Minimum R2_value to consider: ') ; 

    %% Read in information one p/v at a time
    for i = 1: num_pv_points
        %% Note voltage should be subtracted from 118
        v = input('Enter voltage: ') ;


        [file_name, path_name] = uigetfile('C:\data\IMS\DB_Creation\BSADirectInfusion\*_all_drifts.csv', 'Choose drifts file')
        if file_name == 0
            continue ;
        end
        drift_file_name = strcat(path_name, file_name) ;
        lc_ims_data = csvread(drift_file_name, 1, 0 ) ;

        % read mass
        mono_masses = lc_ims_data(:, mono_mass_column) ;
        cs_vector = lc_ims_data(:, cs_column) ;


        % read drift times and correct for pressure
        pressure = lc_ims_data(:, pressure_column) ;
        drift_times = lc_ims_data(:, drift_time_column)  ;
        corr_drift_times = drift_times .* (standard_pressure./pressure(1)) ;
        pv = repmat(standard_pressure/v, 1, size(lc_ims_data,1)) ;

        this_pv_data = [mono_masses cs_vector corr_drift_times pv'] ;

        all_drifts = [all_drifts ; this_pv_data] ;

    end
end

% output header
output_cells = {'Index', 'Feature_Mass', 'CS', 'Crossection', 'Mobility', 'Slope', 'Intercept', 'R2', 'DT_Norm' } ;


% We have all the data right now, so identify common
% features based on mass.
mass_col = 1 ;
cs_col = 2 ;
dt_col = 3 ;
pv_col = 4 ;
sorted_drifts = [] ;
sorted_drifts = sortrows(all_drifts, mass_col) ;
num_peaks = size(sorted_drifts, 1) ;

% Get pv points
pvs = GetUniqueElements(all_drifts(:, pv_col)) ;

current_index = 1 ;
num_ids_so_far = 1 ;

% begin single-linkage clustering
while(current_index <= num_peaks)
    current_peak = sorted_drifts(current_index, :) ;
    umc_masses = [] ;
    umc_drifts= [] ;
    umc_pvs = [] ;
    umc_cs = [] ;

    match_index = current_index + 1;

    if (match_index > num_peaks)
        break ;
    end

    umc_drifts = [umc_drifts ; current_peak(dt_col)] ;
    umc_masses = [umc_masses ; current_peak(mass_col)] ;
    umc_pvs = [umc_pvs ; current_peak(pv_col)] ;
    umc_cs = [umc_cs ; current_peak(cs_col)] ;
  
    mass_tol = ppm_tolerance * current_peak(mass_col)/1000000 ;
    max_mass = current_peak(mass_col) + mass_tol ;
    match_peak = sorted_drifts(match_index, :) ;
    
    if(abs(current_peak(mass_col) - 664.3525)<0.5)
        debug = true ; 
    end

    while (match_peak(mass_col) <= max_mass)% && match_peak(cs_col) == current_peak(cs_col))
        % found a guy with the same mass and same charge
        umc_masses = [umc_masses ; match_peak(mass_col)] ;
        umc_drifts = [umc_drifts ; match_peak(dt_col)] ;
        umc_cs = [umc_cs ; match_peak(cs_col)] ;
        umc_pvs = [umc_pvs; match_peak(pv_col)]  ;
        match_index = match_index + 1;
        if (match_index > num_peaks)
            break ;
        end
        match_peak = sorted_drifts(match_index, :) ;
    end



    num_pv_observed = 0 ;
    observed_pv_drifts = [] ;
    observed_pv_points = [] ;
    observed_cs = [] ;
    umc_rep_mass = mean(umc_masses) ;

    % For each pv point, calculate mean of observed drifts
    for j = 1:size(pvs,1)
        this_pv = pvs(j) ;
        I = find(umc_pvs == this_pv) ;
        if (size(I, 1) > 0)
            umc_dt_time_for_pv = mean(umc_drifts(I)) ;

            %get most observed charge state
            [p_cs, bin_cs] = hist(umc_cs, [1 2 3 4 5 6 7 8 9 10]) ;
            [y_cs, i_cs] = max(p_cs) ;
            cs = bin_cs(i_cs) ;

            observed_pv_drifts = [observed_pv_drifts ; umc_dt_time_for_pv] ;
            observed_pv_points = [observed_pv_points ; this_pv] ;
            observed_cs = [observed_cs ; cs] ;
        end
    end

    if (size(observed_pv_drifts,1) >= min_num_pv_points)
        % start regression
        n = length(observed_pv_points) ;
        x1 = [ones(n,1), observed_pv_points] ;
        [beta, bint, r, rint, stats] = regress(observed_pv_drifts, x1) ;
        slope = beta(2) ;
        intercept = beta(1) ;
        r2 = stats(1) ;
        if (r2 > min_R2_value)
            % see which charge state to choose
            [p_cs, bin_cs] = hist(observed_cs, [1 2 3 4 5 6 7 8 9 10]) ;
            [y_cs, i_cs] = max(p_cs) ;
            cs = bin_cs(i_cs) ;

            %calculate
            [k0 omega] = CalculateCrossSection(cs, slope, avg_temp, umc_rep_mass, drift_tube_length, buffer_gas) ;
            dt_standard = BackCalculateDriftTime(omega, avg_temp, intercept, cs, umc_rep_mass, buffer_gas, (800-88), drift_tube_length, slope) ;


            row = {num_ids_so_far umc_rep_mass cs omega  k0 slope intercept r2 dt_standard} ;
            output_cells = [output_cells ; row] ;
            num_ids_so_far = num_ids_so_far + 1;
        end
    end

    current_index = match_index ;

end
%write out results to a DB-like xls file
cd(path_name) ;
[newmatfile, newpath] = uiputfile('*.xls', 'Save As');
filename = [newpath,newmatfile];
if isempty(findstr([newpath,newmatfile],'.xls'))
    filename = [filename,'.xls'];
end
fid = fopen(filename, 'r') ;
if (fid > -1)
    fclose(fid) ;
    delete(filename) ; % so as to overwrite the damn thing
end
clear fid ;
s = xlswrite(filename, output_cells) ;
cd (current_dir) ;
