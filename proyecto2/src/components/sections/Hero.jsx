import { hero, site } from '../../data/content'
import Button from '../ui/Button'
import Icon from '../ui/Icon'
import HeroVisual from './HeroVisual'
import styles from './Hero.module.css'

export default function Hero() {
  return (
    <section id="inicio" className={styles.hero}>
      <div className={styles.background} aria-hidden="true" />

      <div className={`container ${styles.inner}`}>
        <div className={styles.copy}>
          <a href={site.bookingUrl} className={styles.badge}>
            <span className={styles.badgeDot} />
            {hero.badge}
            <Icon name="arrowRight" size={14} />
          </a>

          <h1 className={styles.title}>
            {hero.titleStart}
            <br />
            <span className="gradient-text">{hero.titleHighlight}</span>
          </h1>

          <p className={styles.subtitle}>{hero.subtitle}</p>

          <div className={styles.ctas}>
            <Button href={site.bookingUrl} size="lg" iconRight="arrowRight">
              {hero.primaryCta}
            </Button>
            <Button href="#como-funciona" variant="secondary" size="lg" onDark>
              {hero.secondaryCta}
            </Button>
          </div>

          <ul className={styles.assurances}>
            {hero.assurances.map((item) => (
              <li key={item}>
                <Icon name="check" size={16} strokeWidth={2.4} />
                {item}
              </li>
            ))}
          </ul>
        </div>

        <HeroVisual />
      </div>

      <div className={`container ${styles.integrations}`}>
        <p className={styles.integrationsLabel}>{hero.integrationsLabel}</p>
        <ul className={styles.integrationsList}>
          {hero.integrations.map((tool) => (
            <li key={tool}>{tool}</li>
          ))}
        </ul>
      </div>
    </section>
  )
}
