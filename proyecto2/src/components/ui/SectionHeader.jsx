import Reveal from './Reveal'
import styles from './SectionHeader.module.css'

/**
 * Cabecera estándar de sección: eyebrow + título + subtítulo.
 * align: 'center' | 'left'
 */
export default function SectionHeader({ eyebrow, title, subtitle, align = 'center', onDark = false }) {
  return (
    <Reveal className={`${styles.header} ${styles[align]} ${onDark ? styles.onDark : ''}`}>
      {eyebrow && <span className={styles.eyebrow}>{eyebrow}</span>}
      <h2 className={styles.title}>{title}</h2>
      {subtitle && <p className={styles.subtitle}>{subtitle}</p>}
    </Reveal>
  )
}
