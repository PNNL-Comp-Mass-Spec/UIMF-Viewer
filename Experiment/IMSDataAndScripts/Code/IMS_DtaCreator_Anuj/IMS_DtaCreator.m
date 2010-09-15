% Program to create a dta files from a parent-fragment spectra
% Reads an isos file for the parent as well as its UMC list file 
% The UMC file is created outside of this program using VIPER.

clear all ;
clc ;
mdbl_mass_H =  1.00727638 ;
cd('C:\DriftTimePrediction\ConformerDetection');

%file_name = '4pep_4T_1.8_600_1500_50ms_adc_parent_fr5_0000.Merge_0000_isos.csv';
%parent_isos_data = csvread(file_name, 1, 0 ) ;

%%read the UMC list for the parent
file_name = '.\BSA_parent\bsa_4T_1.8_500_2000_30ms_parent_0000_UMC.xls';
parent_umc_data = xlsread(file_name);

file_name = '.\BSA_frag\bsa_4T_1.8_500_2000_30ms_polyfrag_0000_UMC.xls';
fragment_umc_data = xlsread(file_name);

%file_name = '4pep_4T_1.8_600_1500_50ms_adc_frag_lin_fr5_0000.Merge_0000_UMCProfiles.xls';
%fragment_umc_profiles = xlsread(file_name);

parent_fragment_matrix = zeros(size(parent_umc_data,1), size(fragment_umc_data,1));
[total_parent_umcs, col] = size(parent_umc_data);
[total_fragment_umcs, col] = size(fragment_umc_data);

%for each fragment, check how many parents can it belong to
%this check is going to be based on scan numbers and it's
%monoisotopic mass.

%The scan numbers cannot go outside the bounds of the parent scan numbers
%while the monoisotopic mass of a fragment cannot be greater than that of
%the parent
for fragment_index = 1: total_fragment_umcs
    fragment = fragment_umc_data(fragment_index,:);
    parent_start_scan = parent_umc_data(:,4);
    parent_end_scan = parent_umc_data(:,5);
    parent_mono_mw = parent_umc_data(:,3);
    fragment_mono_mw = fragment_umc_data (fragment_index,3);
    indices = fragment(:,4) >= parent_start_scan & fragment(:,5) <= parent_end_scan & parent_mono_mw >= fragment_mono_mw;

    parent_fragment_matrix (:,fragment_index) = indices;
end;

%gives an indication of the total number of fragments in the parent
fragmentSum = sum(parent_fragment_matrix,2);


for parent_index = 1: total_parent_umcs
    %%take the fragment sum to figure out how many fragments are present in
    %%this parent
    
    if (parent_umc_data(parent_index,3) <1000)% || (parent_umc_data(parent_index,3) > 1550)
        continue;
    end;
    
    if ( fragmentSum(parent_index,1) == 0) 
        continue;
    else
         output_file_name = 'C:\SequestRunner\4pepdtas\' ;
         str_isos = sprintf('%d', parent_umc_data(parent_index,1 )) ;
         cs = parent_umc_data(parent_index,11 );
         str_cs = sprintf('%d', cs) ;
         output_file_name = strcat(output_file_name, str_cs) ;
         output_file_name = strcat(output_file_name, '.') ;
         output_file_name = strcat(output_file_name, str_isos) ;
         output_file_name = strcat(output_file_name, '.') ;
         output_file_name = strcat(output_file_name, str_isos) ;
         output_file_name = strcat(output_file_name, '.dta') ;
         
         fout = fopen(output_file_name, 'wb') ;
         %the dta file contains the singly protonated peptide mass(MH+) and
         %the peptide charge stats as a pair of space separated values
         fprintf(fout, '%6.3f\t', parent_umc_data(parent_index, 12) + mdbl_mass_H) ;
         %fprintf(fout, '%6.3f\t', parent_umc_data(parent_index, 12)) ;
         fprintf(fout, '%d\n',parent_umc_data(parent_index,11)) ;
         fragment_indices = find(parent_fragment_matrix(parent_index, :)==1);
         for fragmentCounter =1:size(fragment_indices,2)
                fragmentIndex = fragment_indices(fragmentCounter);
                fragment_umc = fragment_umc_data(fragmentIndex,:);
                frag_cs = fragment_umc(:, 11);
                %use this for manual matching with fragments generated from
                %ICR2LS
                fprintf(fout, '%6.3f %6.3f\n', fragment_umc(:,12), fragment_umc(:,14)) ;
                
                %use this line for original dta creation
                %fprintf(fout, '%6.3f %6.3f %d\n', fragment_umc(:,13), fragment_umc(:,14), fragment_umc(:,11)) ;
         end;
         fclose(fout);
                 
    end;
    
end;
