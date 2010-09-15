function out = DetermineShift()

sigma = 10;
gauss_dimension = ceil((4*sigma));
A = 1.60/gauss_dimension;
m = gauss_dimension/2;
for j = 1:gauss_dimension
      gauss(j) = A *exp(-((j-m)^2)/(2*sigma^2));
end

y_clean = zeros(gauss_dimension);
filter = BuildMatchFilter (gauss, 1, sigma, 0);
Y = MatchFilter(gauss, filter);
for j = 2:gauss_dimension-1
    if ((Y(j) > Y(j-1))&& (Y(j) > Y(j-1))) % recognize peak
        peak_y = j;
        break;
    end
end

for j = 2:gauss_dimension-1
    if ((X(j) > X(j-1))&& (X(j) > X(j-1))) % recognize peak
        peak_x = j;
        break;
    end
end
        
out = peak_y - peak_x;