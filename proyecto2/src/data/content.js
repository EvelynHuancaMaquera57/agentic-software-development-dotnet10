/* ==========================================================================
   CONTENIDO DE LA LANDING
   Todo el copy vive aquí. Edita textos, métricas, FAQ o testimonios
   sin tocar los componentes.
   ========================================================================== */

export const site = {
  name: 'Automatiza360',
  // Sustituye por tu enlace real de Calendly / Cal.com / Google Calendar.
  bookingUrl: '#reservar',
  email: 'hola@automatiza360.com',
}

export const nav = {
  links: [
    { label: 'Beneficios', href: '#beneficios' },
    { label: 'Cómo funciona', href: '#como-funciona' },
    { label: 'Casos de uso', href: '#casos' },
    { label: 'Testimonios', href: '#testimonios' },
    { label: 'FAQ', href: '#faq' },
  ],
  cta: 'Reserva una llamada',
}

export const hero = {
  badge: 'Diagnóstico de automatización gratuito',
  titleStart: 'Más ventas,',
  titleHighlight: 'menos tareas manuales.',
  subtitle:
    'Implementamos automatizaciones y agentes de IA que conectan tu WhatsApp, email, CRM y calendario. Cada lead recibe respuesta en minutos, ningún seguimiento se pierde y tu equipo dedica su tiempo a cerrar ventas.',
  primaryCta: 'Reserva una llamada',
  secondaryCta: 'Ver cómo funciona',
  assurances: ['Llamada de 30 min', 'Sin compromiso', 'Plan de acción incluido'],
  integrationsLabel: 'Conectamos las herramientas que ya usas',
  integrations: ['WhatsApp', 'Gmail', 'HubSpot', 'Google Calendar', 'Notion', 'Airtable', 'Stripe'],

  // Visual del hero (tarjeta tipo dashboard)
  flow: {
    title: 'Flujo · Nuevo lead → Reunión',
    status: 'En vivo',
    steps: [
      { icon: 'form', title: 'Nuevo lead desde el formulario', meta: 'Laura M. · Clínica estética', time: '09:41' },
      { icon: 'sparkles', title: 'La IA califica el lead', meta: 'Intención de compra alta', time: '09:41', tag: 'Prioritario' },
      { icon: 'message', title: 'Respuesta enviada por WhatsApp', meta: 'En 48 segundos', time: '09:42' },
      { icon: 'calendar', title: 'Reunión agendada', meta: 'Jueves 11:00 · Google Calendar', time: '09:47' },
      { icon: 'database', title: 'CRM actualizado', meta: 'Oportunidad creada y asignada', time: '09:47' },
    ],
    stats: [
      { label: 'Leads hoy', value: '37' },
      { label: 'Resp. media', value: '1m 12s' },
      { label: 'Reuniones', value: '9' },
    ],
  },
  floatingCards: [
    { icon: 'trending', value: '+32%', label: 'reuniones agendadas' },
    { icon: 'clock', value: '14 h', label: 'recuperadas / semana' },
  ],
}

export const metrics = {
  title: 'Resultados medibles desde las primeras semanas',
  items: [
    { value: '120+', label: 'automatizaciones funcionando en negocios reales' },
    { value: '15 h', label: 'ahorradas por persona cada semana, de media' },
    { value: '< 2 min', label: 'de tiempo de respuesta a cada nuevo lead' },
    { value: '+35%', label: 'más reuniones agendadas en 90 días' },
  ],
}

export const problems = {
  eyebrow: 'El problema',
  title: '¿Tu negocio crece, pero tu equipo no da abasto?',
  subtitle:
    'La mayoría de pymes no pierde ventas por falta de clientes, sino por procesos que dependen de que alguien se acuerde de hacer algo.',
  items: [
    { icon: 'clock', title: 'Leads que se enfrían', text: 'Un contacto que espera horas una respuesta suele acabar comprando a la competencia.' },
    { icon: 'repeat', title: 'Tareas de copiar y pegar', text: 'Pasar datos de formularios al CRM, a hojas de cálculo y al email consume horas cada semana.' },
    { icon: 'alert', title: 'Seguimientos que se olvidan', text: 'Sin un sistema, el seguimiento depende de la memoria de cada persona y de notas sueltas.' },
    { icon: 'link', title: 'Herramientas desconectadas', text: 'WhatsApp, email, CRM y calendario funcionan por separado y nadie tiene la foto completa.' },
    { icon: 'message', title: 'Soporte saturado', text: 'Las mismas preguntas una y otra vez, respondidas a mano, fuera de horario o tarde.' },
    { icon: 'users', title: 'Crecer = contratar', text: 'Cada nuevo cliente añade trabajo operativo, y la única salida parece sumar más personal.' },
  ],
}

export const benefits = {
  eyebrow: 'Beneficios',
  title: 'Un negocio que funciona aunque no estés mirando',
  subtitle:
    'Diseñamos sistemas que trabajan en segundo plano para que tu equipo se dedique a lo que de verdad genera ingresos.',
  items: [
    {
      icon: 'zap',
      title: 'Respuesta inmediata a cada cliente',
      text: 'Tus leads reciben respuesta personalizada en minutos, por WhatsApp o email, a cualquier hora.',
      highlight: 'Respuesta en < 2 min',
      featured: true,
    },
    {
      icon: 'trending',
      title: 'Seguimiento comercial sin huecos',
      text: 'Recordatorios, secuencias y avisos automáticos para que ninguna oportunidad se quede a medias.',
      highlight: '+35% reuniones',
      featured: true,
    },
    { icon: 'clock', title: 'Menos tareas manuales', text: 'Eliminamos el trabajo repetitivo: registros, copias de datos, informes y avisos internos.' },
    { icon: 'layers', title: 'Captación, ventas y soporte conectados', text: 'Un único flujo desde que llega el lead hasta que se convierte en cliente y recibe soporte.' },
    { icon: 'link', title: 'Tus herramientas, por fin integradas', text: 'Conectamos lo que ya usas. Sin migraciones dolorosas ni cambiar la forma de trabajar del equipo.' },
    { icon: 'users', title: 'Más capacidad sin contratar', text: 'Atiende más clientes con el mismo equipo y escala sin que los costes crezcan al mismo ritmo.' },
  ],
}

export const howItWorks = {
  eyebrow: 'Cómo funciona',
  title: 'De procesos manuales a sistemas automáticos en 3 pasos',
  subtitle: 'Un método claro, con resultados visibles desde las primeras semanas.',
  steps: [
    {
      number: '01',
      title: 'Diagnóstico gratuito',
      text: 'En una llamada de 30 minutos analizamos tus procesos, detectamos cuellos de botella y priorizamos las automatizaciones con más impacto.',
      deliverable: 'Mapa de oportunidades priorizado',
    },
    {
      number: '02',
      title: 'Diseño e implementación',
      text: 'Construimos e integramos los flujos y agentes de IA con tus herramientas actuales. Tú validas cada paso antes de ponerlo en marcha.',
      deliverable: 'Primeros flujos activos en 2–4 semanas',
    },
    {
      number: '03',
      title: 'Optimización continua',
      text: 'Medimos resultados, ajustamos lo necesario y formamos a tu equipo para que saque el máximo partido al sistema.',
      deliverable: 'Panel de métricas y soporte',
    },
  ],
}

export const useCases = {
  eyebrow: 'Casos de uso',
  title: 'Automatizaciones que ya están generando resultados',
  subtitle: 'Algunos de los sistemas que implementamos con más frecuencia.',
  items: [
    { icon: 'funnel', category: 'Captación', title: 'Captación automática de leads', text: 'Formularios, anuncios y redes llegan directos a tu CRM, etiquetados y listos para contactar.' },
    { icon: 'message', category: 'Ventas', title: 'Seguimiento por email y WhatsApp', text: 'Secuencias personalizadas que se activan según el comportamiento de cada lead.' },
    { icon: 'calendar', category: 'Ventas', title: 'Agendado automático de reuniones', text: 'El lead elige hueco, recibe confirmación y recordatorios. Sin idas y venidas.' },
    { icon: 'sparkles', category: 'IA', title: 'Clasificación de leads con IA', text: 'La IA analiza cada contacto y prioriza a quienes tienen más probabilidad de comprar.' },
    { icon: 'headset', category: 'Soporte', title: 'Atención al cliente automatizada', text: 'Un agente de IA resuelve dudas frecuentes 24/7 y deriva a tu equipo los casos complejos.' },
    { icon: 'file', category: 'Operaciones', title: 'Presupuestos y recordatorios', text: 'Genera presupuestos a partir de una plantilla, envíalos y haz seguimiento automático de pagos.' },
    { icon: 'database', category: 'Operaciones', title: 'Sincronización CRM ↔ herramientas', text: 'Datos coherentes entre CRM, hojas de cálculo, facturación y gestión de proyectos.' },
  ],
  custom: {
    title: '¿Tu proceso es diferente?',
    text: 'Diseñamos automatizaciones a medida para tu forma de trabajar.',
    cta: 'Cuéntanos tu caso',
  },
}

export const testimonials = {
  eyebrow: 'Testimonios',
  title: 'Negocios que ya trabajan de otra forma',
  items: [
    {
      quote:
        'Antes respondíamos a los leads al día siguiente. Ahora reciben respuesta en minutos y las reuniones agendadas se han disparado. Es como tener a alguien más en el equipo comercial.',
      name: 'Carolina Ruiz',
      role: 'Directora · Clínica Dermalia',
      result: '+41% reuniones',
    },
    {
      quote:
        'Gestionábamos los clientes entre WhatsApp, Excel y el email. Automatiza360 lo conectó todo y ahora sabemos en qué punto está cada proyecto sin preguntar a nadie.',
      name: 'Javier Morales',
      role: 'Fundador · Agencia Nexo Digital',
      result: '12 h/semana ahorradas',
    },
    {
      quote:
        'El agente de IA responde el 70% de las dudas de mis alumnos. Yo me centro en crear contenido y lanzar, no en contestar los mismos mensajes cada día.',
      name: 'Andrea Salinas',
      role: 'Infoproductora · Academia Impulsa',
      result: '70% consultas resueltas',
    },
  ],
}

export const faq = {
  eyebrow: 'Preguntas frecuentes',
  title: 'Resolvemos tus dudas',
  items: [
    {
      q: '¿Necesito conocimientos técnicos?',
      a: 'No. Nos encargamos del diseño, la implementación y las integraciones. Tú solo nos cuentas cómo funciona tu negocio y validas cada paso.',
    },
    {
      q: '¿Qué incluye el diagnóstico gratuito?',
      a: 'Una llamada de 30 minutos en la que revisamos tus procesos de captación, ventas y soporte. Sales con una lista priorizada de automatizaciones, el impacto estimado y los siguientes pasos, decidas o no trabajar con nosotros.',
    },
    {
      q: '¿Con qué herramientas trabajáis?',
      a: 'WhatsApp Business, Gmail, Outlook, HubSpot, Pipedrive, Notion, Airtable, Google Sheets, Calendly, Stripe y cientos más mediante integraciones y APIs. Si ya usas una herramienta, lo más probable es que podamos conectarla.',
    },
    {
      q: '¿Cuánto tarda la implementación?',
      a: 'Las primeras automatizaciones suelen estar activas en 2 a 4 semanas. Proyectos más amplios se dividen en fases para que veas resultados desde el principio.',
    },
    {
      q: '¿La IA puede responder mal a mis clientes?',
      a: 'Configuramos los agentes con tu información, tu tono y reglas claras. Cuando una consulta lo requiere, la conversación pasa a una persona de tu equipo. Tú mantienes siempre el control.',
    },
    {
      q: '¿Cuánto cuesta?',
      a: 'Depende del alcance. Tras el diagnóstico te enviamos una propuesta cerrada, con precio y plazos definidos, sin sorpresas.',
    },
    {
      q: '¿Mis datos están seguros?',
      a: 'Sí. Trabajamos con herramientas que cumplen RGPD, accesos con permisos mínimos y documentación de cada flujo implementado.',
    },
  ],
}

export const finalCta = {
  eyebrow: 'Diagnóstico gratuito',
  title: 'Descubre cuántas horas puede recuperar tu negocio este mes',
  subtitle:
    'Reserva una llamada estratégica de 30 minutos. Analizamos tus procesos y te llevas un plan concreto de automatización, sin compromiso.',
  checklist: [
    'Detectamos tus principales cuellos de botella',
    'Identificamos tareas repetitivas que se pueden automatizar',
    'Estimamos el impacto en tiempo y ventas',
  ],
  cta: 'Reserva una llamada',
  note: 'Plazas limitadas cada semana para garantizar la calidad del análisis.',
}

export const footer = {
  tagline: 'Automatizaciones e inteligencia artificial para pymes que quieren crecer sin multiplicar el trabajo manual.',
  columns: [
    {
      title: 'Soluciones',
      links: [
        { label: 'Captación de leads', href: '#casos' },
        { label: 'Seguimiento comercial', href: '#casos' },
        { label: 'Atención con IA', href: '#casos' },
        { label: 'Integraciones', href: '#beneficios' },
      ],
    },
    {
      title: 'Empresa',
      links: [
        { label: 'Cómo funciona', href: '#como-funciona' },
        { label: 'Testimonios', href: '#testimonios' },
        { label: 'Preguntas frecuentes', href: '#faq' },
        { label: 'Contacto', href: '#reservar' },
      ],
    },
    {
      title: 'Legal',
      links: [
        { label: 'Aviso legal', href: '#' },
        { label: 'Privacidad', href: '#' },
        { label: 'Cookies', href: '#' },
      ],
    },
  ],
}
