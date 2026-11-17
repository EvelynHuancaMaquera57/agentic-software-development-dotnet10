import { metrics } from '../../data/content'
import Reveal from '../ui/Reveal'
import styles from './Metrics.module.css'

export default function Metrics() {
  return (
    <section className={styles.metrics} aria-label="Resultados">
      <div className="container">
        <Reveal as="p" className={styles.title}>
          {metrics.title}
        </Reveal>
        <div className={styles.grid}>
          {metrics.items.map((item, i) => (
            <Reveal key={item.label} delay={i * 80} className={styles.item}>
              <span className={styles.value}>{item.value}</span>
              <span className={styles.label}>{item.label}</span>
            </Reveal>
          ))}
        </div>
      </div>
    </section>
  )
}
