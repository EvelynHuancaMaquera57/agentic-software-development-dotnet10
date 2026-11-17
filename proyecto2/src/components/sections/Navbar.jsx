import { useEffect, useState } from 'react'
import { nav, site } from '../../data/content'
import Button from '../ui/Button'
import Icon from '../ui/Icon'
import Logo from '../ui/Logo'
import styles from './Navbar.module.css'

export default function Navbar() {
  const [scrolled, setScrolled] = useState(false)
  const [open, setOpen] = useState(false)

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 12)
    onScroll()
    window.addEventListener('scroll', onScroll, { passive: true })
    return () => window.removeEventListener('scroll', onScroll)
  }, [])

  // Bloquea el scroll del body con el menú móvil abierto
  useEffect(() => {
    document.body.style.overflow = open ? 'hidden' : ''
    return () => {
      document.body.style.overflow = ''
    }
  }, [open])

  const close = () => setOpen(false)

  return (
    <header className={`${styles.nav} ${scrolled || open ? styles.scrolled : ''}`}>
      <div className={`container ${styles.inner}`}>
        <Logo />

        <nav className={styles.links} aria-label="Principal">
          {nav.links.map((link) => (
            <a key={link.href} href={link.href} className={styles.link}>
              {link.label}
            </a>
          ))}
        </nav>

        <div className={styles.actions}>
          <Button href={site.bookingUrl} size="md" className={styles.cta}>
            {nav.cta}
          </Button>
          <button
            className={styles.toggle}
            onClick={() => setOpen((v) => !v)}
            aria-expanded={open}
            aria-controls="mobile-menu"
            aria-label={open ? 'Cerrar menú' : 'Abrir menú'}
          >
            <Icon name={open ? 'close' : 'menu'} size={22} />
          </button>
        </div>
      </div>

      <div id="mobile-menu" className={`${styles.mobile} ${open ? styles.mobileOpen : ''}`}>
        <nav className="container" aria-label="Móvil">
          {nav.links.map((link) => (
            <a key={link.href} href={link.href} className={styles.mobileLink} onClick={close}>
              {link.label}
            </a>
          ))}
          <Button href={site.bookingUrl} size="lg" iconRight="arrowRight" onClick={close} className={styles.mobileCta}>
            {nav.cta}
          </Button>
        </nav>
      </div>
    </header>
  )
}
