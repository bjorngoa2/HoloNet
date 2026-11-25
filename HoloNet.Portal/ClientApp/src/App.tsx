import { useQuery } from '@tanstack/react-query'

interface PortalConfig {
  videoUrl: string
  photosUrl: string
  gamesUrl: string
}

interface ServiceCard {
  title: string
  description: string
  emoji: string
  url: string
  apiPath: string
}

async function fetchConfig(signal?: AbortSignal): Promise<PortalConfig> {
  const res = await fetch('/api/v1/config', { signal })
  if (!res.ok) throw new Error(`Config fetch failed: ${res.status}`)
  return res.json()
}

async function fetchCount(baseUrl: string, apiPath: string, signal?: AbortSignal): Promise<number> {
  const res = await fetch(`${baseUrl}${apiPath}`, { signal })
  if (!res.ok) throw new Error('Failed to fetch')
  const data: unknown[] = await res.json()
  return data.length
}

function ServiceCardComponent({ service }: { service: ServiceCard }) {
  const { data: count } = useQuery({
    queryKey: ['count', service.apiPath],
    queryFn: ({ signal }) => fetchCount(service.url, service.apiPath, signal),
    retry: false,
  })

  return (
    <a
      href={service.url}
      target="_blank"
      rel="noreferrer"
      className="flex items-center gap-4 p-5 rounded-xl border border-white/10 bg-white/5 hover:border-violet-500/60 hover:bg-white/10 hover:-translate-y-0.5 transition-all duration-200 no-underline group"
    >
      <span className="text-4xl shrink-0">{service.emoji}</span>

      <div className="flex-1 min-w-0">
        <h2 className="text-base font-semibold text-white">{service.title}</h2>
        <p className="mt-0.5 text-sm text-white/50 leading-snug">{service.description}</p>
        {count !== undefined && (
          <span className="inline-block mt-2 px-2 py-0.5 rounded-full text-xs border border-violet-500/30 bg-violet-500/10 text-violet-300">
            {count} items
          </span>
        )}
      </div>

      <span className="text-white/30 group-hover:text-violet-400 group-hover:translate-x-1 transition-all duration-200 text-lg">
        →
      </span>
    </a>
  )
}

function App() {
  const { data: config, isLoading, error } = useQuery({
    queryKey: ['config'],
    queryFn: ({ signal }) => fetchConfig(signal),
  })

  const services: ServiceCard[] = config
    ? [
        {
          title: 'Videos',
          description: 'Browse and stream your video library',
          emoji: '🎬',
          url: config.videoUrl,
          apiPath: '/api/v1/videos',
        },
        {
          title: 'Photos',
          description: 'Browse your photo collection',
          emoji: '📷',
          url: config.photosUrl,
          apiPath: '/api/v1/photos',
        },
        {
          title: 'Games',
          description: 'Browse your retro game library',
          emoji: '🎮',
          url: config.gamesUrl,
          apiPath: '/api/v1/games',
        },
      ]
    : []

  return (
    <div className="min-h-screen bg-[#0d0d12] text-white">
      <div className="max-w-2xl mx-auto px-6 py-16">
        <header className="text-center mb-12">
          <h1 className="text-4xl font-extrabold tracking-tight bg-gradient-to-r from-violet-400 to-purple-300 bg-clip-text text-transparent">
            HoloNet
          </h1>
          <p className="mt-2 text-white/40 text-sm">Your personal homelab</p>
        </header>

        <main>
          {isLoading && (
            <p className="text-center text-white/40 py-12">Loading…</p>
          )}
          {error && (
            <p className="text-center text-red-400 py-12">
              ⚠️ Could not load config: {String(error)}
            </p>
          )}
          {config && (
            <div className="flex flex-col gap-3">
              {services.map(service => (
                <ServiceCardComponent key={service.title} service={service} />
              ))}
            </div>
          )}
        </main>
      </div>
    </div>
  )
}

export default App


