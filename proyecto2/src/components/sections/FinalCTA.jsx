import { finalCta, site } from '../../data/content'
import Button from '../ui/Button'
import Icon from '../ui/Icon'
import Reveal from '../ui/Reveal'
import styles from './FinalCTA.module.css'

export default function FinalCTA() {
  return (
    <section id="reservar" className={styles.wrapper}>
      <div className="container">
        <Reveal className={styles.panel}>
          <div className={styles.background} aria-hidden="true" />

          <div className={styles.content}>
            <span className={styles.eyebrow}>{finalCta.eyebrow}</span>
            <h2 className={styles.title}>{finalCta.title}</h2>
            <p className={styles.subtitle}>{finalCta.subtitle}</p>

            <ul className={styles.checklist}>
              {finalCta.checklist.map((item) => (
                <li key={item}>
                  <span className={styles.check}>
                    <Icon name="check" size={14} strokeWidth={3} />
                  </span>
                  {item}
                </li>
              ))}
            </ul>

            <div className={styles.actions}>
              <Button href={site.bookingUrl} variant="white" size="lg" iconRight="arrowRight">
                {finalCta.cta}
              </Button>
              <p className={styles.note}>{finalCta.note}</p>
            </div>
          </div>
        </Reveal>
      </div>
    </section>
  )
}
