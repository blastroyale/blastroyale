float calculateCamAlphaScale(float3 viewPos, float farPlaneDist, float3 camPos)
{
	farPlaneDist *= (0.15f * (1000.0f / farPlaneDist));
	farPlaneDist *= max(1.0f, abs(camPos.y) / 10.0f);
	return saturate(1.0f - abs(viewPos.z) / farPlaneDist);
}