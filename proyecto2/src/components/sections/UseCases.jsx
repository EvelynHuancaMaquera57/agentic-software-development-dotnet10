import { site, useCases } from '../../data/content'
import Icon from '../ui/Icon'
import Reveal from '../ui/Reveal'
import SectionHeader from '../ui/SectionHeader'
import styles from './UseCases.module.css'

export default function UseCases() {
  return (
    <section id="casos" className="section">
      <div className="container">
        <SectionHeader eyebrow={useCases.eyebrow} title={useCases.title} subtitle={useCases.subtitle} />

        <div className={styles.grid}>
          {useCases.items.map((item, i) => (
            <Reveal key={item.title} delay={(i % 4) * 70} className={styles.cell}>
              <article className={styles.card}>
                <div className={styles.cardTop}>
                  <span className={styles.icon}>
                    <Icon name={item.icon} size={20} />
                  </span>
                  <span className={styles.category}>{item.category}</span>
                </div>
                <h3 className={styles.title}>{item.title}</h3>
                <p className={styles.text}>{item.text}</p>
              </article>
            </Reveal>
          ))}

          <Reveal delay={210} className={styles.cell}>
            <a href={site.bookingUrl} className={`${styles.card} ${styles.custom}`}>
              <span className={styles.customIcon}>
                <Icon name="plus" size={20} />
              </span>
              <h3 className={styles.title}>{useCases.custom.title}</h3>
              <p className={styles.text}>{useCases.custom.text}</p>
              <span className={styles.customLink}>
                {useCases.custom.cta}
                <Icon name="arrowRight" size={16} />
              </span>
            </a>
          </Reveal>
        </div>
      </div>
    </section>
  )
}
