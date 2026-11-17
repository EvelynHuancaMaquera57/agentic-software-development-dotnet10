import { footer, site } from '../../data/content'
import Icon from '../ui/Icon'
import Logo from '../ui/Logo'
import styles from './Footer.module.css'

export default function Footer() {
  const year = new Date().getFullYear()

  return (
    <footer className={styles.footer}>
      <div className={`container ${styles.top}`}>
        <div className={styles.brand}>
          <Logo />
          <p className={styles.tagline}>{footer.tagline}</p>
          <a href={`mailto:${site.email}`} className={styles.email}>
            <Icon name="mail" size={18} />
            {site.email}
          </a>
        </div>

        {footer.columns.map((col) => (
          <nav key={col.title} className={styles.column} aria-label={col.title}>
            <h4 className={styles.columnTitle}>{col.title}</h4>
            <ul>
              {col.links.map((link) => (
                <li key={link.label}>
                  <a href={link.href} className={styles.link}>
                    {link.label}
                  </a>
                </li>
              ))}
            </ul>
          </nav>
        ))}
      </div>

      <div className={`container ${styles.bottom}`}>
        <p>
          © {year} {site.name}. Todos los derechos reservados.
        </p>
        <a href="#" className={styles.social} aria-label="LinkedIn">
          <Icon name="linkedin" size={18} />
        </a>
      </div>
    </footer>
  )
}
