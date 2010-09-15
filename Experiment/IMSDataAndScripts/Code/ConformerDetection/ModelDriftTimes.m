% Program to model drift time profiles using the mclust program.  This is
% an attempt at discovering and quantifying conformers.
%
% Two files are the main input
%              - Dataset_UMCs.xls - the Excel version of the VIPER _UMCs.txt
%                file
%              - Dataset_drifts.csv - the corresponding _drifts file
%


%**************************************************************************
%*************************************************************************


%% Initialize
ppm_tolerance = 20 ;
continue_process = 'y' ;
debug = 0 ;
standard_pressure = 4 ;

%% column numbers in input _UMC.xls file
%% As of May 27, 2008, there are two ways that you can get _UMCs.xls files
%% from VIPER - one is through a database search (report LCMS features)
%% and the second is through edit->Copy LCMS features
%% This version assumes that the _UMCS.xls was created from Option 1

amt_viper_mass_tag_mass_column = 25  ;
amt_viper_mass_tag_id_column = 24 ;
amt_viper_net_error_column = 29 ;
amt_viper_mass_tag_net_column = 38 ;  %matlab is weird
amt_viper_mass_tag_peptide_column  = 34 ;
amt_viper_mass_tag_protein_column = 38 ;
amt_viper_umc_mass_column = 6 ;
amt_viper_umc_scan_column = 4 ;
amt_viper_umc_net_column = 5 ;
amt_viper_umc_charge_column = 11 ;
amt_viper_umc_scan_start_column = 2 ;
amt_viper_umc_scan_end_column = 3 ;


%% column numbers in input _drifts.csv file
ims_scan_column = 2 ;
drift_time_column  = 3 ;
lc_scan_column = 1 ;
mono_mass_column = 8 ;
fit_column = 9 ;
cs_column = 5 ;
abundance_column = 6 ;
pressure_column = 11  ;

matched_mass = [] ;

current_dir = pwd ;

lc_scans = [] ;

read = 0;
if (read ==1)
    clc ;

    %load VIPER output
    [file_name, path_name]   = uigetfile('*.xls', 'Choose results file from VIPER AMT peak matching') ;
    if file_name == 0
        return ;
    end
    cd(path_name) ;
    pkmatched_file_name = strcat(path_name, file_name) ;
    [pkmatched, txt] = xlsread(pkmatched_file_name) ;

    % Append net information as this is absent in method-one-_UMCs.xls
    mt_nets = pkmatched(:, amt_viper_umc_net_column) + pkmatched(:, amt_viper_net_error_column) ;
    pkmatched = [pkmatched mt_nets] ;

    % ask for drifts file pertaining to that rep and read in drift file data
    [file_name, path_name] = uigetfile('*_all_drifts.csv', 'Choose corresponding drifts file')
    if file_name == 0
        continue ;
    end
    drift_file_name = strcat(path_name, file_name) ;
    lc_ims_data = csvread(drift_file_name, 1, 0 ) ;
end


% get all MTIDs and keep only unique ones
all_mass_tag_ids = pkmatched(:, amt_viper_mass_tag_id_column) ;
I_mass_tags = find(all_mass_tag_ids>0) ;
id_mass_tag_ids = all_mass_tag_ids(I_mass_tags) ;
cd(current_dir) ;
mass_tag_ids = GetUniqueElements(id_mass_tag_ids) ;
cd(path_name) ;
num_pkmatched = size(mass_tag_ids, 1) ;
%finalResults = [0 0] ;

% do for all MTIDS
for j = 1:num_pkmatched

    this_mass_tag_id = mass_tag_ids(j) ;

    %   this_mass_tag_id = 87531 ;
    %     this_mass_tag_id = 328388 ;
    this_mass_tag_id = 1758;

    I_mass_tags = find(pkmatched(:,amt_viper_mass_tag_id_column) == this_mass_tag_id);

    sprintf ('Reading MTID # %d', this_mass_tag_id)

    % get MTID mass
    this_mass_tag_mass = pkmatched(I_mass_tags(1), amt_viper_mass_tag_mass_column) ;

    % get mass to which MTID matched
    matched_masses = pkmatched(I_mass_tags, amt_viper_umc_mass_column) ;

    % get lc_scans that MTID was observed in
    lc_scans = pkmatched(I_mass_tags, amt_viper_umc_scan_column) ;
    lc_scan_start = pkmatched(I_mass_tags, amt_viper_umc_scan_start_column) ;
    lc_scan_end = pkmatched(I_mass_tags, amt_viper_umc_scan_end_column) ;

    num_lc_scans = size(lc_scans, 1) ;

    % for TIC
    maxTIC = 0;
    maxTIC_lc_scan = 0 ;
    maxTIC_index_mass = 0 ;
    maxTIC_index_lc_scan = 0 ;

    % %     % find lc_scan with max TIC
    % %     for k = 1:num_lc_scans
    % %         this_lc_num  = lc_scans(k) ;
    % %         this_matched_mass = matched_masses(k) ;
    % %         index_lc_scan = find(lc_ims_data(:, lc_scan_column) == this_lc_num) ;
    % %         ims_data = lc_ims_data(index_lc_scan, :) ;
    % %
    % %         % find the entry that has same mass as the matched_mass
    % %         ppm = abs(ims_data(:, mono_mass_column) - this_matched_mass)/this_matched_mass ;
    % %         ppm = ppm * 1000000 ;
    % %         index_mass = find(ppm < ppm_tolerance);
    % %
    % %         if (size(index_mass, 1) > 0)
    % %             %located! calculate TIC
    % %             TIC = sum(ims_data(index_mass, abundance_column)) ;
    % %             if TIC > maxTIC
    % %                 maxTIC = TIC ;
    % %                 maxTIC_lc_scan = this_lc_num ;
    % %                 maxTIC_index_mass = index_mass ;
    % %                 maxTIC_index_lc_scan = index_lc_scan ;
    % %             end
    % %         end
    % %     end
    % %
    % %     if (maxTIC > 0)
    % %         % found scan
    % %         this_lc_num = maxTIC_lc_scan ;
    % %         iScan = find(lc_scans == this_lc_num) ;
    % %
    % %         % read data again
    % %         ims_data = [] ;
    % %         ims_data = lc_ims_data(maxTIC_index_lc_scan, :) ;
    % %         pressure = ims_data(maxTIC_index_mass(1), pressure_column) ; % since pressure is same acrros a lc_scan
    % %         drift_times = ims_data(maxTIC_index_mass, drift_time_column)  ;
    % %         % correct for pressure
    % %         drift_times = drift_times .* (standard_pressure./pressure);
    % %         intensity_values = ims_data(maxTIC_index_mass, abundance_column) ;
    % %         mass_values = ims_data(maxTIC_index_mass, mono_mass_column) ;
    % %         % model drift time
    % %         cd(current_dir) ;
    % %         [resolved_mass_tags, resolved_drift_times]= ResolveConformers(drift_times, intensity_values, mass_values, this_mass_tag_id, 1) ;
    % %         %finalResults = [finalResults; resolved_mass_tags' resolved_drift_times'];
    % %     end

    %% Doing a variation where the intensities are summed across all UMC
    %% scans

%     for k = 1: num_lc_scans
%         this_scan_start = lc_scan_start(k) ;
%         this_scan_end = lc_scan_end(k) ;
%         this_matched_mass = matched_masses(k) ;
% 
%         minDriftTime = 0 ;
%         maxDriftTime = 70 ;
%         minMass = 200 ;
%         maxMass = 5000 ;
% 
%         numBins = 1000 ;
%         binDrifts = linspace(minDriftTime, maxDriftTime, 100) ;
%         binIntensities = zeros(100, 1) ;
%         binMasses = linspace(minMass, maxMass, 100) ;
% 
%         for scan = this_scan_start:this_scan_end
%             scanIndex = find(lc_ims_data(:, lc_scan_column) == scan) ;
%             ims_data = lc_ims_data(scanIndex, :) ;
% 
%             % find the entry that has the same mass
%             ppm = abs(ims_data(:, mono_mass_column) - this_matched_mass)/this_matched_mass ;
%             ppm = ppm * 1000000 ;
%             index_mass = find(ppm < ppm_tolerance);
% 
%             if(size(index_mass, 1) > 0)
%                 pressure = ims_data(index_mass(1), pressure_column);
%                 drift_times = ims_data(index_mass, drift_time_column) ;
%                 drift_times = drift_times .* (standard_pressure./pressure) ;
%                 intensity_values = ims_data(index_mass, abundance_column) ;
%                 mass_values = ims_data(index_mass, mono_mass_column) ;
% 
%             end
% 
%         end
% 
%     end
end



