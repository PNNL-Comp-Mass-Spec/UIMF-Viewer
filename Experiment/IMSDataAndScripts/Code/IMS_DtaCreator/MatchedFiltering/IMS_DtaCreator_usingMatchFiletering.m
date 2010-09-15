%%clear command window and all variables
clc
clear all

% The matrices are oriented differently
% from the PLS way, with scan on the x
% and mz on the y
mbln_ms_read_data = 0;
mbln_ms_ms_read_data = 1 ;
mbln_isos_data = 1 ;
-+
mbln_concatenate_dtas = 0 ;
mbln_use_match_filtering = 1;

mint_min_scan = 1 ;
mint_max_scan = 700 ;
mint_num_scans = mint_max_scan - mint_min_scan ;
mdbl_min_mz = 100;
mdbl_max_mz = 2000 ;
mdbl_delta_mz  = 0.1 ;
mdbl_mass_H =  1.00727638 ;
mint_num_bins = floor((mdbl_max_mz-mdbl_min_mz)/mdbl_delta_mz) ;

format long  ;

if mbln_isos_data
    % read in deisotoped data
    marr_isos_mz_list = [] ;
    marr_isos_mono_list = [] ;
    marr_isos_cs_list =[] ;
    display('Processing Isos File') ;
    ms_isos_file_name = 'C:\DriftTimePrediction\MS_MS\4pep_4T_1.8_600_1500_50ms_adc_parent_fr5_0000\4pep_4T_1.8_600_1500_50ms_adc_parent_fr5_0000.Merge_0000_isos.csv' ; 
    M = csvread(ms_isos_file_name, 1, 0) ;
    
    %%filter data based on M/Z values
    I = find((M(:, 4) > mdbl_min_mz) & (M(:, 4) < mdbl_max_mz)) ;
    
    %% get all matching m/z, monoisotopic masses, charge states, ims scan
    %% numbers and abundances
    marr_isos_ims_scan = M(I, 1) ;  
    marr_isos_cs_list = M(I, 2) ;
    marr_isos_abundance = M(I, 3) ;
    marr_isos_mz_list = M(I, 4) ;
    marr_isos_mono_list = M(I, 7) ;
    %save all this data in binary format in matlab.mat file
    save marr_all_isos marr_isos_mz_list marr_isos_mono_list marr_isos_cs_list marr_isos_ims_scan marr_isos_abundance ;
else
    display('Loading Isotopes') ;
    load marr_all_isos ;
    display('Done') ;
end

if mbln_ms_ms_read_data
    % matrix ims_scan on x axis, m/z on y axis and intensity as value
    mmat_ms_ms_matrix = [] ;
    mmat_ms_ms_matrix = zeros(mint_num_bins, mint_num_scans) ;
    display('Processing MS/MS frame') ;
%    ms_ms_file_name = 'C:\data\IMS\IMS-MSMS\4pep_4T_1.6_650_10000_oct14_lin7_0000\MS_MS_run.txt' ;
%   ms_ms_file_name = 'C:\data\IMS\IMS-MSMS\FromPaper\4pep_4T_1.6_650_10000_oct15_lin7_118_0000\Frame_1\4pep_frag_lin7.txt ' ; 
    ms_ms_file_name = 'C:\DriftTimePrediction\MS_MS\4pep_4T_1.8_600_1500_50ms_adc_frag_lin_fr5_0000\4pep_4T_1.8_600_1500_50ms_adc_frag_lin_fr5_0000.Merge_0000_isos.csv' ;
    fms_ms_spectra = fopen(ms_ms_file_name);
    %Read off header
    line = fgets(fms_ms_spectra);
    %Start reading data
    line = fgets(fms_ms_spectra);
    while(line ~= -1)
        value = str2num(line);
        intensity = value(2) ;
        mz = value(3) ;
        ims_scan = value(4) ;
        if (mz > mdbl_min_mz && mz < mdbl_max_mz)
            mz_index = floor((mz-mdbl_min_mz)/mdbl_delta_mz) + 1 ;
            mmat_ms_ms_matrix(mz_index, ims_scan + 1) =  mmat_ms_ms_matrix(mz_index, ims_scan + 1) + intensity ;
        end
        line = fgets(fms_ms_spectra);
    end
    fclose(fms_ms_spectra) ;
    save mmat_ms_ms_matrix mmat_ms_ms_matrix ;
    display('Done') ;
else
    display('Loading MS_MS frame') ;
    load mmat_ms_ms_matrix ;
    display('Done') ;
end

plot = 0
if plot
    setYaxes = [mdbl_min_mz : mdbl_delta_mz: (mdbl_max_mz-mdbl_delta_mz)];
    setXaxes = [mint_min_scan: mint_max_scan-1];
    figure ;
    surf(setXaxes, setYaxes, log(1+mmat_ms_ms_matrix), 'EdgeColor', 'flat') ;
    view(2);
    colormap('hot');
end

if mbln_ms_read_data
    mmat_ms_matrix = [] ;
    mmat_ms_matrix = zeros(mint_num_bins, mint_num_scans) ;
    display ('Processing MS frame') ;
    ms_file_name = 'C:\data\IMS\IMS-MSMS\4pep_4T_1.6_650_10000_37_0001\MS_run.txt' ;
    fms_spectra = fopen(ms_file_name);
    %Read off header
    line = fgets(fms_spectra);
    %Start reading data
    line = fgets(fms_spectra);
    while(line ~= -1)
        value = str2num(line);
        intensity = value(2) ;
        mz = value(3) ;
        ims_scan = value(4) ;
        if (mz > mdbl_min_mz && mz < mdbl_max_mz)
            mz_index = floor((mz-mdbl_min_mz)/mdbl_delta_mz) + 1 ;
            mmat_ms_matrix(mz_index, ims_scan+1) =  mmat_ms_ms_matrix(mz_index, ims_scan+1) + intensity ;
        end
        line = fgets(fms_spectra);
    end
    fclose(fms_spectra) ;
    save mmat_ms_matrix mmat_ms_matrix ;
    display('Done') ;
else
    display('Loading MS frame') ;
    load mmat_ms_matrix ;
    display('Done') ;
end


num_isos =  size(marr_isos_mz_list, 1) ;

display ('Creating DTAs') ;
if mbln_use_match_filtering
    for isos_num = 1: num_isos
        mono_mz = marr_isos_mz_list(isos_num) ;
        mono_mz_index = floor((mono_mz  - mdbl_min_mz)/mdbl_delta_mz)+1 ;
        mono_mw = marr_isos_mono_list(isos_num) ;
        mono_cs  = marr_isos_cs_list(isos_num)  ;
        mono_scan = marr_isos_ims_scan(isos_num) ;
        mono_abundance = marr_isos_abundance(isos_num)  ;
        
       
        mono_mz_bin = mmat_ms_matrix(mono_mz_index, :) ;
        while(size(find(mono_mz_bin > 0), 2) == 0)
            mono_mz_index = mono_mz_index - 1;
            mono_mz_bin = mmat_ms_matrix(mono_mz_index, :) ;
        end

        R_value = [] ;
        for i = 1: mint_num_bins
            fragment_mz_bin = mmat_ms_ms_matrix(i,:) ;
            if ((size(find(fragment_mz_bin > 0)) > 0 ))
                R = corrcoef(mono_mz_bin, fragment_mz_bin) ;
                R_value(i) = R(1,2) ;
            else
                R_value(i) = 0 ;
            end
        end

        % Get the drift profile of parent
        ms_channel = mono_mz_bin ;
        ms_channel_smooth = butterworth(ms_channel) ;
        snr_ms = SignalNoiseRatio(ms_channel_smooth) ;
        [ims_peak_value, ims_peak_index] = FindPeaks(ms_channel_smooth, snr_ms) ;
        diff = abs(ims_peak_index - mono_scan) ;
        min_diff = min(diff) ;
        ms_scan_index = ims_peak_index(find(diff == min_diff)) ;

        if size(ms_scan_index, 2) > 1
            ms_scan_index = ms_scan_index(1) ;
        end

        %Start Matched filtering
        sigma = FullWidthHalfMax(ms_channel_smooth, ms_scan_index) ;
        filter = BuildMatchFilter(ms_channel_smooth, snr_ms, sigma, 0) ;

        all_ms_ms_indices = find(R_value > 0.4) ;

        if (size(all_ms_ms_indices, 2) >0)
            max_R = max(R_value) ;
            maxrindex = find(R_value == max_R) ;
            ms_max = mmat_ms_ms_matrix(maxrindex, :) ;
            %plot(ms_max) ;

            %output ;
            str1 = 'C:\data\IMS\IMS-MSMS\PLS_out\4pep_MatchFilter' ;
            str_isos = sprintf('%d', isos_num) ;
            str_cs = sprintf('%d', mono_cs) ;
            str1  = strcat(str1, '.') ;
            str1 = strcat(str1, str_isos) ;
            str1  = strcat(str1, '.') ;
            str1 = strcat(str1, str_isos) ;
            str1  = strcat(str1, '.') ;
            str1 = strcat(str1, str_cs) ;
            str1 = strcat(str1, '.dta') ;
            fout = fopen(str1, 'wb') ;

            sprintf('Processing scan # %d', mono_scan)
            fprintf(fout, '%6.3f\t', mono_mw + mdbl_mass_H) ;
            fprintf(fout, '%d\n', mono_cs) ;

            %fragments
            for j = 1: size(all_ms_ms_indices, 2) ;
                ms_ms_mz_index = all_ms_ms_indices(j) ;
                ms_ms_channel = mmat_ms_ms_matrix(ms_ms_mz_index, :) ;
                y = MatchFilter(ms_ms_channel, filter) ;
                % reality_check
                fragment_intensity = GetIntensity(y, ms_ms_channel)  ;
                if (fragment_intensity > 0)
                    fragment_mz = mdbl_min_mz + (ms_ms_mz_index * mdbl_delta_mz) ;
                    fprintf(fout, '%6.3f %6.3f\n', fragment_mz, fragment_intensity) ;
                else
                    debug = 1 ;
                end
            end

            fclose(fout) ;
        end
    end
else
    % Simple algorithm
    for isos_num = 1: num_isos
        mono_mz = marr_isos_mz_list(isos_num) ;
        mono_mz_index = floor((mono_mz  - mdbl_min_mz)/mdbl_delta_mz)+1 ;
        mono_mw = marr_isos_mono_list(isos_num) ;
        mono_cs  = marr_isos_cs_list(isos_num)  ;
        mono_scan = marr_isos_ims_scan(isos_num) ;
       
       
        str1 = 'C:\data\IMS\IMS-MSMS\DriftMatch_out\4pep_drift_matching' ;
        str_isos = sprintf('%d', isos_num) ;
        str_cs = sprintf('%d', mono_cs) ;        
        str_scan = sprintf('%d', mono_scan) ; 
        str1  = strcat(str1, '_') ;
        str1 = strcat(str1, str_scan) ; 
        str1  = strcat(str1, '.') ;
        str1 = strcat(str1, str_isos) ;
        str1  = strcat(str1, '.') ;
        str1 = strcat(str1, str_isos) ;
        str1  = strcat(str1, '.') ;
        str1 = strcat(str1, str_cs) ;
        str1 = strcat(str1, '.dta') ;
        fout = fopen(str1, 'wb') ;
        sprintf('Processing scan # %d', mono_scan)
        fprintf(fout, '%6.3f\t', mono_mw + mdbl_mass_H) ;
        fprintf(fout, '%d\n', mono_cs) ;

        %fragments
        ms_ms_channel = sum(mmat_ms_ms_matrix(:, mono_scan-3:mono_scan+3), 2) ;
     %   max_fragment_intensity = max(ms_ms_channel)
      %  ms_ms_channel = (ms_ms_channel/max_fragment_intensity) * 100 ; 
        for i = 1: size(ms_ms_channel)            
            fragment_intensity = ms_ms_channel(i);
            if (fragment_intensity > 0)
                fragment_mz = mdbl_min_mz + (i * mdbl_delta_mz) ;
                fprintf(fout, '%6.3f %6.3f\n', fragment_mz, fragment_intensity) ;
            end
        end
        fclose(fout) ;
    end
end