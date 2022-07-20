inline float circle(in float2 _st, in float _radius)
{
	float2 dist = _st - float2(0.5, 0.5);
	return 1. - smoothstep(_radius - (_radius * 0.01),
	                       _radius + (_radius * 0.01),
	                       dot(dist, dist) * 4.0);
}