% Program to calculate drift time for peptide identifications from a VIPER
% analysis.
%
% Two files are the main input
%              - Dataset_UMCs.xls - the Excel version of the VIPER _UMCs.txt
%                file
%              - Dataset_drifts.csv - the corresponding _drifts file
%
% Reps are allowed
% Output is in the form of xls document 

%**************************************************************************
%*************************************************************************

%% Initialize
clc ;
clear all
ppm_tolerance = 50 ;
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
amt_viper_mass_tag_peptide_column  = 33 ; 
amt_viper_umc_mass_column = 6 ;
amt_viper_umc_scan_column = 4 ;
amt_viper_umc_net_column = 5 ; 
amt_viper_umc_charge_column = 11 ;

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
all_mass_tag_ids = [] ;
mass_tag_ids = [] ;
mass_tag_masses= [] ;
mass_tag_nets = [] ;

matched_mass = [] ;
all_drifts = [] ;

identified_mass_tags = [] ;
identified_mass_tag_mass = [] ;
identified_mass_tag_net = [] ;
identified_mass_tag_peptide = [] ;
identified_mass_tag_protein = [] ;

% output header
output_cells = {'Mass_Tag_ID', 'Peptide_Name', 'ORF_Name', 'Mass_Tag_Mass', 'Mass_Tag_NET', 'Drift_Time', 'Charge_State', 'P/V'} ;

current_dir = pwd ;

%% Enter stuff
%% Note voltage should be subtracted from 118
V = input ('Voltage:    ') ;
num_reps =input ('Number of Reps: ') ;
lc_scans = [] ;

% do for all reps
for i = 1:num_reps
    %load VIPER output and read in data
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

    % get all MTIDs and keep only unique ones
    all_mass_tag_ids = pkmatched(:, amt_viper_mass_tag_id_column) ;
    I_mass_tags = find(all_mass_tag_ids>0) ;
    id_mass_tag_ids = all_mass_tag_ids(I_mass_tags) ;
    cd(current_dir) ;
    mass_tag_ids = unique(id_mass_tag_ids) ;
    cd(path_name) ;
    num_pkmatched = size(mass_tag_ids, 1) ;

    % do for all MTIDS
    for j = 1:num_pkmatched
        this_mass_tag_id = mass_tag_ids(j) ;
       
        I_mass_tags = find(pkmatched(:,amt_viper_mass_tag_id_column) == this_mass_tag_id);

        sprintf ('Reading MTID # %d', this_mass_tag_id)

        % get MTID mass
        this_mass_tag_mass = pkmatched(I_mass_tags(1), amt_viper_mass_tag_mass_column) ;
        
        % get peptide and protein id
        this_mass_tag_peptide = txt(I_mass_tags(1), amt_viper_mass_tag_peptide_column) ; 
        %this_mass_tag_protein = txt(I_mass_tags(1), amt_viper_mass_tag_protein_column) ; 

        % get lc_scans that MTID was observed in
        lc_scans = pkmatched(I_mass_tags, amt_viper_umc_scan_column) ;
        lc_nets = pkmatched(I_mass_tags, amt_viper_mass_tag_net_column) ;
        num_lc_scans = size(lc_scans, 1) ;

        % get mass to which MTID matched
        matched_masses = pkmatched(I_mass_tags, amt_viper_umc_mass_column) ;

        % for TIC
        maxTIC = 0;
        maxTIC_lc_scan = 0 ;
        maxTIC_index_mass = 0 ;
        maxTIC_index_lc_scan = 0 ;

        % find lc_scan with max TIC
        for k = 1:num_lc_scans
            this_lc_num  = lc_scans(k) ;
            this_matched_mass = matched_masses(k) ;
            
            index_lc_scan = find(lc_ims_data(:, lc_scan_column) == this_lc_num) ;
            ims_data = lc_ims_data(index_lc_scan, :) ;

            % find the entry that has same mass as the matched_mass
            ppm = abs(ims_data(:, mono_mass_column) - this_matched_mass)/this_matched_mass ; 
            ppm = ppm * 1000000 ;
            
            
            ppm_mz = abs((ims_data(:, mono_mass_column)/ims_data(:,cs_column)) - this_matched_mass)/this_matched_mass ; 
            ppm_mz = ppm_mz * 1000000 ;
            index_mass = find(ppm < ppm_tolerance);
            index_mz = find(ppm < ppm_tolerance);
            
            if (size(index_mass, 1) > 0)
                %located! calculate TIC
                TIC = sum(ims_data(index_mass, abundance_column)) ;
                if TIC > maxTIC
                    maxTIC = TIC ;
                    maxTIC_lc_scan = this_lc_num ;
                    maxTIC_index_mass = index_mass ;
                    maxTIC_index_lc_scan = index_lc_scan ;
                end
            end
        end

        if (maxTIC > 0)
            % found scan, now get mt_net
            this_lc_num = maxTIC_lc_scan ;
            iScan = find(lc_scans == this_lc_num) ;
            this_mass_tag_net = lc_nets(iScan) ;

            % read data again
            ims_data = [] ;
            ims_data = lc_ims_data(maxTIC_index_lc_scan, :) ;
            pressure = ims_data(maxTIC_index_mass(1), pressure_column) ; % since pressure is same acrros a lc_scan
            drift_times = ims_data(maxTIC_index_mass, drift_time_column)  ;
            % correct for pressure
            drift_times = drift_times .* (standard_pressure./pressure);
            intensity_values = ims_data(maxTIC_index_mass, abundance_column) ;
            
            if size(drift_times,1) > 4
                    A = [drift_times intensity_values];
                    filename = ['DriftTimePeaks' num2str(j) '.txt'];
                    save( filename, 'A','-ASCII');
            end
            
            % model drift time
             cd(current_dir) ;
            [resolved_mass_tags, peak_drift_times]= CalculatePeakDriftTimesOld(drift_times, intensity_values, this_mass_tag_id) ;

            
            % get all observed charge states and see which is the most frequent
            charge_state_vector = ims_data(maxTIC_index_mass, cs_column) ;
            [p_cs, bin_cs] = hist(charge_state_vector, [1 2 3 4 5 6 7 8 9 10]) ; 
            [y_cs, i_cs] = max(p_cs) ; 
            charge_state = bin_cs(i_cs) ;

            cd(path_name) ;

            for k = 1: size(peak_drift_times, 1)
                all_drifts = [all_drifts ; i resolved_mass_tags(k) peak_drift_times(k) charge_state standard_pressure this_lc_num] ;
            end

            % store mass tag id as being successfully stored
            Iindex = find(identified_mass_tags == this_mass_tag_id);
            if(size(Iindex, 1) == 0)
                % not present already, so add it
                identified_mass_tags = [identified_mass_tags ; this_mass_tag_id] ;
                identified_mass_tag_mass = [identified_mass_tag_mass ; this_mass_tag_mass] ;
                identified_mass_tag_net = [identified_mass_tag_net ; this_mass_tag_net] ;
                identified_mass_tag_peptide = [identified_mass_tag_peptide ; this_mass_tag_peptide] ; % need to change
                identified_mass_tag_protein = [identified_mass_tag_protein ; this_mass_tag_protein] ; % need to change
            end
        end
    end
end


num_identified = size(identified_mass_tags, 1) ;

% all replicates done for this voltage value
% now for each mass_tag_id, get a vector of drift time values that
% are seen across replicates
if (size(all_drifts, 1) > 0)
    for i = 1: num_identified        
        this_mass_tag_id = identified_mass_tags(i) ;
        this_mass_tag_mass =identified_mass_tag_mass(i) ;
        this_mass_tag_net = identified_mass_tag_net(i) ;
        this_mass_tag_peptide = identified_mass_tag_peptide{i} ;
        this_mass_tag_protein = identified_mass_tag_protein{i} ;

        sprintf ('Processing MTID # %d', this_mass_tag_id)
        
       
        % find mass tage info in all_drifts
        I = find (all_drifts(:, 2) == this_mass_tag_id) ;
        if (size(I, 1) == 1)
            % seen only once in only one replicate
            drift_time = all_drifts(I, 3);
            cs = all_drifts(I, 4) ;
            PV = standard_pressure/V ;
            lc = all_drifts(I, 6) ; 
            row = {this_mass_tag_id this_mass_tag_peptide this_mass_tag_protein this_mass_tag_mass this_mass_tag_net drift_time cs PV} ;
            output_cells = [output_cells; row] ;
        end
        if (size(I, 1) > 1)
            % seen more than in one replicate, thus we need to standardize
            % again
            drift_mean = mean(all_drifts(I, 3)) ; % Take mean of DT
            charge_vector_across_replicates = all_drifts(I, 4) ; % take most observed cs
            [p_cs, bin_cs] = hist(charge_vector_across_replicates, [1 2 3 4 5 6 7 8 9 10]) ; 
            [y_cs, i_cs] = max(p_cs) ; 
            final_charge_state = bin_cs(i_cs) ;            
            PV = standard_pressure/V ; % p/v 
            row = {this_mass_tag_id this_mass_tag_peptide this_mass_tag_protein this_mass_tag_mass this_mass_tag_net drift_mean final_charge_state PV} ;
            output_cells = [output_cells; row] ;
        end
    end
end

% next voltage
cd(path_name) ;


% write out results
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

