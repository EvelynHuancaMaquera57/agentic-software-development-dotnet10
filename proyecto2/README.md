# Automatiza360 · Landing page

Landing en React + Vite orientada a conseguir reservas de llamada.

## Arrancar

```bash
npm install
npm run dev      # http://localhost:5173
npm run build    # build de producción en dist/
npm run preview  # sirve el build
```

## Dónde editar

| Quiero cambiar…                         | Archivo                                   |
| --------------------------------------- | ----------------------------------------- |
| Textos, métricas, FAQ, testimonios      | `src/data/content.js`                     |
| Enlace de reserva (Calendly, Cal.com…)  | `site.bookingUrl` en `src/data/content.js` |
| Colores, tipografía, espaciados, radios | `src/styles/tokens.css`                   |
| Orden de secciones                      | `src/App.jsx`                             |
| Estilo de una sección concreta          | `src/components/sections/<Sección>.module.css` |
| Botones, cabeceras de sección           | `src/components/ui/`                      |

Las clases CSS aparecen en DevTools con nombre legible (`Hero_title__xY12`), así que
es fácil localizar qué regla tocar. Los tokens se pueden ajustar en vivo desde `:root`.
