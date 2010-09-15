clc
clear all

mbln_ms_read_data = 0 ;
mbln_train = 1 ;
mbln_ms_ms_read_data = 0 ;
mbln_isos_data = 0 ;
mbln_concatenate_dtas = 0 ; 


mint_min_scan = 1 ;
mint_max_scan = 700 ;
mint_num_scans = mint_max_scan - mint_min_scan ;
mdbl_min_mz = 200;
mdbl_max_mz = 1000 ;
mdbl_delta_mz  = 0.1 ;
mdbl_mass_H =  1.00727638 ; 
mint_num_bins = floor((mdbl_max_mz-mdbl_min_mz)/mdbl_delta_mz) ;
mdbl_ms_ms_min_mz = mdbl_min_mz ;
mdbl_ms_ms_max_mz = mdbl_max_mz ;

format long  ;

if mbln_isos_data
    % read in deisotoped data
    marr_isos_mz_list = [] ;
    marr_isos_mono_list = [] ;
    marr_isos_cs_list =[] ;
    display('Processing Isos File') ;
    ms_isos_file_name = 'C:\data\IMS\IMS-MSMS\4pep_4T_1.6_650_10000_37_0001\4pep_4T_1.6_650_10000_37_0001.Accum_1_isos.csv' ;
    M = csvread(ms_isos_file_name, 1, 0) ;
    I = find((M(:, 4) > mdbl_min_mz) & (M(:, 4) < mdbl_max_mz)) ;
    marr_isos_mz_list = M(I, 4) ;
    marr_isos_mono_list = M(I, 7) ;
    marr_isos_cs_list = M(I, 2) ;
    marr_isos_ims_scan = M(I, 1) ; 
    marr_isos_abundance = M(I, 3) ; 
    save marr_all_isos marr_isos_mz_list marr_isos_mono_list marr_isos_cs_list marr_isos_ims_scan marr_isos_abundance ;
else
    display('Loading Isotopes') ; 
    load marr_all_isos ;
    display('Done') ;
end

if mbln_ms_ms_read_data
    % matrix ims_scan on y axis, m/z on x axis and intensity as value
    mmat_ms_ms_matrix = [] ;
    mmat_ms_ms_matrix = zeros(mint_num_scans, mint_num_bins) ;    
    display('Processing MS/MS frame') ;
    ms_ms_file_name = 'C:\data\IMS\IMS-MSMS\4pep_4T_1.6_650_10000_oct14_lin7_0000\MS_MS_run.txt' ;
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
            mmat_ms_ms_matrix(ims_scan+1,mz_index) =  mmat_ms_ms_matrix(ims_scan+1, mz_index) + intensity ;            
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

if mbln_ms_read_data
    mmat_ms_matrix = [] ;
    mmat_ms_matrix = zeros(mint_num_scans, mint_num_bins) ;
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
            mmat_ms_matrix(ims_scan+1,mz_index) =  mmat_ms_matrix(ims_scan+1, mz_index) + intensity ;
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

% Plot functions
plot = 0;
if (plot)
    setYaxes = [mdbl_min_mz : mdbl_delta_mz: (mdbl_max_mz-mdbl_delta_mz)];
    setXaxes = [mint_min_scan: mint_max_scan-1];
    surf(setYaxes, setXaxes, log(1+mmat_ms_matrix), 'EdgeColor', 'flat') ;
    view(2);
    colormap('hot');
    figure ;
    surf(setYaxes, setXaxes, log(1+mmat_ms_ms_matrix), 'EdgeColor', 'flat') ;
    view(2);
    colormap('hot');
end

if mbln_train
    display('Begin training') ; 
    X = [] ;
    Y = [] ;
    % Option 1 - Take out all colums which are empty in the beginning i.e
    % crop on m/z
    % do it to Y first to retain min and max mz values
%     [Y, mdbl_min_ms_ms_mz, mdbl_max_ms_ms_mz] = TrimMatrixAlongDimension(mmat_ms_ms_matrix, 2, mdbl_min_mz, mdbl_max_mz, mdbl_delta_mz) ; 
%     [X, mdbl_min_mz, mdbl_max_mz] = TrimMatrixAlongDimension(mmat_ms_matrix, 2, mdbl_min_mz, mdbl_max_mz, mdbl_delta_mz) ; 
    
    
    %Option 2 - Take out all rows which are empty at the beginning and at
    %the end i.e. crop along ims dimension
    % This would mean trim only the fragmentation data and use those values
    % for the MS data, as the number of rows have to be the same
%     [Y, mint_min_scan, mint_max_scan] = TrimMatrixAlongDimension(mmat_ms_ms_matrix, 1, mint_min_scan, mint_max_scan, 1) ; 
%     X = mmat_ms_matrix([mint_min_scan+1: mint_max_scan], :) ; 
%     mdbl_ms_ms_min_mz = mdbl_min_mz ; 
%     mdbl_ms_ms_max_mz = mdbl_max_mz ; 


    % Option 3 -   Consider all values 
    X = mmat_ms_matrix  ; 
    Y = mmat_ms_ms_matrix ;     
    clear mmat_ms_matrix  mmat_ms_ms_matrix  ;
    num_factors_to_keep = rank(X)  ;

    Yhat = [] ;
    [T, P, C, Bpls]= PLS_NIPALS(X, Y, num_factors_to_keep);    

    save mall_trained T P C Bpls X Y mdbl_min_mz mdbl_max_mz mdbl_ms_ms_min_mz mdbl_ms_ms_max_mz
    display('Done') ; 
else
    display('Load Trained data') ; 
    load mall_trained ; 
    display('Done') ;
end

% All done, now get out partial intensities to creat dtas
num_isos =  size(marr_isos_mz_list, 1) ;
s_y = std(Y) ; 
m_y = mean(Y) ; 
X = zscore(X) ;  % so as to get in the same dimensions of the coefficient

if mbln_concatenate_dtas
    display('Creating concatenated DTA') ; 
    file_name = 'C:\data\IMS\IMS-MSMS\4pep_all.txt' ;    
    char_design_front = '================================================================="  ' ; 
    char_design_back  = '" =================================================================' ; 
    fout = fopen(file_name, 'wb') ;    
    for isos_num = 1: num_isos
        % get mz location in X matrix
        mono_mz = marr_isos_mz_list(isos_num) ;
        mono_mz_index = floor((mono_mz  - mdbl_min_mz)/mdbl_delta_mz)+1 ;
        mono_mw = marr_isos_mono_list(isos_num) ;
        mono_cs  = marr_isos_cs_list(isos_num)  ;    
        

        %write out monomass and cs to dta
        str1 = '4pep_' ;
        str_pep = sprintf('%d', isos_num) ;
        str_cs = sprintf('%d', mono_cs) ;
        str1 = strcat(str1, str_pep) ;
        str1  = strcat(str1, '.') ;
        str1 = strcat(str1, str_pep) ; 
        str1  = strcat(str1, '.') ;
        str1 = strcat(str1, str_pep) ; 
        str1  = strcat(str1, '.') ;        
        str1 = strcat(str1, str_cs) ;
        str1 = strcat(str1, '.dta') ;
        
        str_title = [] ; 
        str_title = strcat(char_design_front, str1) ; 
        str_title = strcat(str_title, char_design_back) ; 
        
        fprintf(fout, '%s\n', str_title) ; 
        fprintf(fout, '%6.3f\t', mono_mw + mdbl_mass_H) ;
        fprintf(fout, '%d\n', mono_cs) ;

        % Now the fragments
        % get max intensity of peptide (sum intensities just to be sure)
        mono_max_intensity = max(X(:, mono_mz_index)) ;
        if (mono_max_intensity < max(X(:, mono_mz_index+1)))
            mono_max_intensity = max(X(:, mono_mz_index+1)) ;
            mono_mz_index = mono_mz_index + 1  ;
        end
        if (mono_max_intensity < max(X(:, mono_mz_index-1)))
            mono_max_intensity =  max(X(:, mono_mz_index-1)) ;
            mono_mz_index = mono_mz_index - 1 ;
        end


        if (mono_max_intensity == 0)
            display('Wrong Index') ;
        end


        % get dissociation coefficients now
        fragment_coeffs = Bpls(mono_mz_index, :) ;
        fragment_intensity = fragment_coeffs * mono_max_intensity;        
        
        % Normalizing
        I = find(fragment_intensity < 0) ; 
        fragment_intensity(I) = 0 ; 
        max_frag_intensity = max(fragment_intensity) ; 
        fragment_intensity = (fragment_intensity * 100)/max_frag_intensity ; 
                
        for i = 1:size(fragment_coeffs,2)
            fragment_mz = mdbl_ms_ms_min_mz + ((i-1) * mdbl_delta_mz) ;
            fprintf(fout, '%6.3f %6.3f\n', fragment_mz, fragment_intensity(i)) ;
        end
        fprintf(fout, '\n\n\n') ; 
    end    
    fclose(fout) ;     
else
    display('Creating DTAs') ; 
    for isos_num = 1: num_isos          
        % get mz location in X matrix
        mono_mz = marr_isos_mz_list(isos_num) ;
        mono_mz_index = floor((mono_mz  - mdbl_min_mz)/mdbl_delta_mz)+1 ;
        mono_mw = marr_isos_mono_list(isos_num) ;
        mono_cs  = marr_isos_cs_list(isos_num)  ;
        mono_scan = marr_isos_ims_scan(isos_num) ; 
        mono_abundance = marr_isos_abundance(isos_num)  ; 
        X_abundance = max(max(X(mono_scan-1:mono_scan+1, mono_mz_index-1:mono_mz_index+1))) ; 

        %write out monomass and cs to dta
        str1 = 'C:\data\IMS\IMS-MSMS\PLS_out\4pep_PLS' ;
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

        % Now the fragments
        % get max intensity of peptide
% %         mono_max_intensity = max(X(:, mono_mz_index)) ;      
% %         I_mono_max_intensity = find(X(:, mono_mz_index)== mono_max_intensity) ; 
% %         
% %         if (size(I_mono_max_intensity, 1) > 1)
% %             display 'Error!! more than one max_intensity in drift profile'
% %             fclose(fout) ;            
% %             isos_num
% %             continue
% %         end
% %             
% %         sum_fragment_intensity = 0 ; 
% %        
% %        % get dissociation coefficients now

% %         fragment_coeffs = Bpls(I_mono_max_intensity, :) ;
% %         fragment_intensity = fragment_coeffs * mono_max_intensity ;        

        %% If using just plain isos_file
        fragment_coeffs = Bpls(mono_mz_index, :) ; 
        fragment_intensity = fragment_coeffs * X_abundance ; 

        fragment_intensity = fragment_intensity .* s_y; 
        fragment_intensity = fragment_intensity+ m_y; 
        
        % Normalizing
        I = find(fragment_intensity < 0) ; 
        fragment_intensity(I) = 0 ; 
        max_frag_intensity = max(fragment_intensity) ; 
        if (max_frag_intensity == 0)
            isos_num
            break 
        end
        fragment_intensity = (fragment_intensity * 100)/max_frag_intensity ; 

        
        for i = 1:size(fragment_coeffs,2)
            fragment_mz = mdbl_ms_ms_min_mz + ((i-1) * mdbl_delta_mz) ;
            fprintf(fout, '%6.3f %6.3f\n', fragment_mz, fragment_intensity(i)) ;
        end
        fclose(fout) ;
    end
end


display('Done') ; 





