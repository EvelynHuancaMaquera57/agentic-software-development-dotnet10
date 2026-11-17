import { useState } from 'react'
import { faq } from '../../data/content'
import Icon from '../ui/Icon'
import Reveal from '../ui/Reveal'
import SectionHeader from '../ui/SectionHeader'
import styles from './FAQ.module.css'

export default function FAQ() {
  const [openIndex, setOpenIndex] = useState(0)

  return (
    <section id="faq" className="section">
      <div className={`container ${styles.layout}`}>
        <SectionHeader eyebrow={faq.eyebrow} title={faq.title} align="left" />

        <Reveal className={styles.list}>
          {faq.items.map((item, i) => {
            const isOpen = openIndex === i
            const id = `faq-${i}`
            return (
              <div key={item.q} className={`${styles.item} ${isOpen ? styles.open : ''}`}>
                <h3>
                  <button
                    className={styles.question}
                    aria-expanded={isOpen}
                    aria-controls={id}
                    onClick={() => setOpenIndex(isOpen ? -1 : i)}
                  >
                    {item.q}
                    <span className={styles.toggle}>
                      <Icon name="plus" size={18} />
                    </span>
                  </button>
                </h3>
                <div id={id} className={styles.answerWrap} role="region">
                  <div className={styles.answerInner}>
                    <p className={styles.answer}>{item.a}</p>
                  </div>
                </div>
              </div>
            )
          })}
        </Reveal>
      </div>
    </section>
  )
}
