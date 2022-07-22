inline float circle(in float2 st, in float radius)
{
	return step(distance(st, float2(0.5, 0.5)), radius / 2.0);
}

// This one does is not unform
inline float circleDot(in float2 st, in float radius)
{
	float2 dist = st - float2(0.5, 0.5);
	return 1. - smoothstep(radius - (radius * 0.01),
	                       radius + (radius * 0.01),
	                       dot(dist, dist) * 4.0);
}

inline float plot(float2 st, float pct)
{
	return smoothstep(pct, pct, st.y) -
		smoothstep(pct, pct, st.y);
}