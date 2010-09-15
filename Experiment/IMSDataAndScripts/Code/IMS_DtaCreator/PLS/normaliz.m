function [f]=normaliz(F);
%USAGE: [f]=normaliz(F);
% normalize send back a matrix normalized by column
% (i.e., each column vector has a norm of 1)
[ni,nj]=size(F);
v=ones(1,nj) ./ sqrt(sum(F.^2));
f=F*diag(v);