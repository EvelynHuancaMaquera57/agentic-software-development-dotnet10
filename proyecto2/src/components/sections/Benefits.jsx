import { benefits } from '../../data/content'
import Icon from '../ui/Icon'
import Reveal from '../ui/Reveal'
import SectionHeader from '../ui/SectionHeader'
import styles from './Benefits.module.css'

export default function Benefits() {
  return (
    <section id="beneficios" className="section">
      <div className="container">
        <SectionHeader eyebrow={benefits.eyebrow} title={benefits.title} subtitle={benefits.subtitle} />

        <div className={styles.grid}>
          {benefits.items.map((item, i) => (
            <Reveal
              key={item.title}
              delay={(i % 3) * 90}
              className={`${styles.cell} ${item.featured ? styles.featuredCell : ''}`}
            >
              <article className={`${styles.card} ${item.featured ? styles.featured : ''}`}>
                <span className={styles.icon}>
                  <Icon name={item.icon} size={22} />
                </span>
                <h3 className={styles.title}>{item.title}</h3>
                <p className={styles.text}>{item.text}</p>
                {item.highlight && <span className={styles.highlight}>{item.highlight}</span>}
              </article>
            </Reveal>
          ))}
        </div>
      </div>
    </section>
  )
}
