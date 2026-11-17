import { howItWorks, site } from '../../data/content'
import Button from '../ui/Button'
import Icon from '../ui/Icon'
import Reveal from '../ui/Reveal'
import SectionHeader from '../ui/SectionHeader'
import styles from './HowItWorks.module.css'

export default function HowItWorks() {
  return (
    <section id="como-funciona" className="section section--subtle">
      <div className="container">
        <SectionHeader eyebrow={howItWorks.eyebrow} title={howItWorks.title} subtitle={howItWorks.subtitle} />

        <ol className={styles.steps}>
          {howItWorks.steps.map((step, i) => (
            <Reveal as="li" key={step.number} delay={i * 120} className={styles.step}>
              <div className={styles.top}>
                <span className={styles.number}>{step.number}</span>
                {i < howItWorks.steps.length - 1 && <span className={styles.connector} aria-hidden="true" />}
              </div>
              <h3 className={styles.title}>{step.title}</h3>
              <p className={styles.text}>{step.text}</p>
              <p className={styles.deliverable}>
                <Icon name="check" size={16} strokeWidth={2.4} />
                {step.deliverable}
              </p>
            </Reveal>
          ))}
        </ol>

        <Reveal className={styles.cta}>
          <Button href={site.bookingUrl} size="lg" iconRight="arrowRight">
            Empieza por el diagnóstico gratuito
          </Button>
        </Reveal>
      </div>
    </section>
  )
}
