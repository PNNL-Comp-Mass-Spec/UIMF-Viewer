% Program to create a dta files from a parent-fragment spectra
% Reads an isos file for the parent as well as its UMC list file 
% The UMC file is created outside of this program using VIPER.

clear all ;
clc ;
mdbl_mass_H =  1.00727638 ;
cd('C:\DriftTimePrediction\MS_MS\DataAfterSoftwareCorrection\4\4pep_4T_1.8_600_2000_30ms_parent_0000');

%%read the UMC list for the parent
file_name = '4pep_4T_1.8_600_2000_30ms_parent_0000.Accum_1_UMC.xls';
parent_umc_data = xlsread(file_name);

cd('C:\DriftTimePrediction\MS_MS\DataAfterSoftwareCorrection\4\4pep_4T_1.8_600_2000_30ms_frag_0001');
file_name = '4pep_4T_1.8_600_2000_30ms_frag_0001.Accum_1_isos.csv';
fragment_isos_data = csvread(file_name, 1, 0 ) ;

[total_parent_umcs, col] = size(parent_umc_data);
[total_fragment_data, col] = size(fragment_isos_data);

%for each fragment, check how many parents can it belong to
%this check is going to be based on scan numbers and it's
%monoisotopic mass.

%gives an indication of the total number of fragments in the parent



for parent_index = 1: total_parent_umcs
    if (parent_umc_data(parent_index,3) <1000)% || (parent_umc_data(parent_index,3) > 1550)
        continue;
    end;
    output_file_name = 'C:\SequestRunner\4pepdtas\Dataset' ;
    str_isos = sprintf('%d', parent_umc_data(parent_index,1 ));
    cs = parent_umc_data(parent_index,11 );
    str_cs = sprintf('%d', cs) ;
    output_file_name = strcat(output_file_name, str_cs) ;
    output_file_name = strcat(output_file_name, '.') ;
    output_file_name = strcat(output_file_name, str_isos) ;
    output_file_name = strcat(output_file_name, '.') ;
    output_file_name = strcat(output_file_name, str_isos) ;
    output_file_name = strcat(output_file_name, '.dta') ;
    fout = fopen(output_file_name, 'wb') ;
    
    parent_start_scan = parent_umc_data(parent_index, 4);
    parent_end_scan = parent_umc_data(parent_index, 5);
    %the dta file contains the singly protonated peptide mass(MH+) and
    %the peptide charge stats as a pair of space separated values
    
    fprintf(fout, '%6.3f\t', parent_umc_data(parent_index, 12) + mdbl_mass_H) ;    
    fprintf(fout, '%d\n', parent_umc_data(parent_index, 11)) ;
    
    fragment_indices = find( fragment_isos_data(:,1) >= parent_start_scan &   fragment_isos_data(:,1) <= parent_end_scan);
    [r,c] = size(fragment_indices);
    if r == 0
        continue;
    end;
    %%now print out the possible fragments for this parent
    for fragmentCounter =1:r
                fragmentIndex = fragment_indices(fragmentCounter);
                
                %if fragment_isos_data(fragmentIndex,3) > 100
                    %use this line for original dta creation
                    fprintf(fout, '%6.3f %d\n', fragment_isos_data(fragmentIndex,4), fragment_isos_data(fragmentIndex,3)) ;
                %end;
    end;
 
    fclose(fout);

end;
