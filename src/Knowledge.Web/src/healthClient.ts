export type HealthStatus = 'Healthy' | 'Degraded' | 'Unhealthy'

export interface HealthCheck {
  name: string
  status: HealthStatus
  description: string | null
  data: Record<string, unknown>
}

export interface HealthResponse {
  status: HealthStatus
  checks: HealthCheck[]
}

export async function getReadiness(signal?: AbortSignal): Promise<HealthResponse> {
  const response = await fetch('/health/ready', {
    headers: { Accept: 'application/json' },
    signal,
  })

  if (!response.ok) {
    throw new Error(`Server readiness returned ${response.status}`)
  }

  return (await response.json()) as HealthResponse
}
