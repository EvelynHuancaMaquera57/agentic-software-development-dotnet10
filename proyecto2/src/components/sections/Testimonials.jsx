import { testimonials } from '../../data/content'
import Reveal from '../ui/Reveal'
import SectionHeader from '../ui/SectionHeader'
import styles from './Testimonials.module.css'

const initials = (name) =>
  name
    .split(' ')
    .map((part) => part[0])
    .slice(0, 2)
    .join('')

export default function Testimonials() {
  return (
    <section id="testimonios" className="section section--subtle">
      <div className="container">
        <SectionHeader eyebrow={testimonials.eyebrow} title={testimonials.title} />

        <div className={styles.grid}>
          {testimonials.items.map((item, i) => (
            <Reveal key={item.name} delay={i * 100} className={styles.cell}>
              <figure className={styles.card}>
                <span className={styles.result}>{item.result}</span>
                <blockquote className={styles.quote}>“{item.quote}”</blockquote>
                <figcaption className={styles.author}>
                  <span className={styles.avatar} aria-hidden="true">
                    {initials(item.name)}
                  </span>
                  <span>
                    <span className={styles.name}>{item.name}</span>
                    <span className={styles.role}>{item.role}</span>
                  </span>
                </figcaption>
              </figure>
            </Reveal>
          ))}
        </div>
      </div>
    </section>
  )
}
