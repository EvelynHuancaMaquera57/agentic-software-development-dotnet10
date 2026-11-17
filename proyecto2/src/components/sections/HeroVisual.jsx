import { hero } from '../../data/content'
import Icon from '../ui/Icon'
import styles from './HeroVisual.module.css'

/* Tarjeta tipo dashboard que muestra un flujo de automatización en vivo.
   Los datos se editan en data/content.js → hero.flow */
export default function HeroVisual() {
  const { flow, floatingCards } = hero

  return (
    <div className={styles.wrapper} aria-hidden="true">
      <div className={styles.glow} />

      <div className={styles.window}>
        <div className={styles.header}>
          <div className={styles.dots}>
            <span />
            <span />
            <span />
          </div>
          <span className={styles.headerTitle}>{flow.title}</span>
          <span className={styles.status}>
            <span className={styles.statusDot} />
            {flow.status}
          </span>
        </div>

        <ol className={styles.steps}>
          {flow.steps.map((step, i) => (
            <li key={step.title} className={styles.step} style={{ '--i': i }}>
              <span className={styles.stepIcon}>
                <Icon name={step.icon} size={18} />
              </span>
              <div className={styles.stepBody}>
                <span className={styles.stepTitle}>
                  {step.title}
                  {step.tag && <span className={styles.tag}>{step.tag}</span>}
                </span>
                <span className={styles.stepMeta}>{step.meta}</span>
              </div>
              <span className={styles.stepTime}>{step.time}</span>
              <span className={styles.stepCheck}>
                <Icon name="check" size={12} strokeWidth={3} />
              </span>
            </li>
          ))}
        </ol>

        <div className={styles.stats}>
          {flow.stats.map((stat) => (
            <div key={stat.label} className={styles.stat}>
              <span className={styles.statValue}>{stat.value}</span>
              <span className={styles.statLabel}>{stat.label}</span>
            </div>
          ))}
        </div>
      </div>

      {floatingCards.map((card, i) => (
        <div key={card.label} className={`${styles.floating} ${styles[`floating${i + 1}`]}`}>
          <span className={styles.floatingIcon}>
            <Icon name={card.icon} size={18} />
          </span>
          <div>
            <span className={styles.floatingValue}>{card.value}</span>
            <span className={styles.floatingLabel}>{card.label}</span>
          </div>
        </div>
      ))}
    </div>
  )
}
