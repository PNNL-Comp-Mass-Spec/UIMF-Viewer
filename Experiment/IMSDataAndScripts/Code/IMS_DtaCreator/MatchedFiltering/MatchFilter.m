
function Y = MatchFilter(X, filt)
Y = filter(filt, 1, X);