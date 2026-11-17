import { problems } from '../../data/content'
import Icon from '../ui/Icon'
import Reveal from '../ui/Reveal'
import SectionHeader from '../ui/SectionHeader'
import styles from './Problems.module.css'

export default function Problems() {
  return (
    <section id="problemas" className="section section--subtle">
      <div className="container">
        <SectionHeader eyebrow={problems.eyebrow} title={problems.title} subtitle={problems.subtitle} />

        <div className={styles.grid}>
          {problems.items.map((item, i) => (
            <Reveal key={item.title} delay={(i % 3) * 90} className={styles.cell}>
              <article className={styles.card}>
                <span className={styles.icon}>
                  <Icon name={item.icon} size={20} />
                </span>
                <h3 className={styles.title}>{item.title}</h3>
                <p className={styles.text}>{item.text}</p>
              </article>
            </Reveal>
          ))}
        </div>
      </div>
    </section>
  )
}
