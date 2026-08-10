import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import en from './en/common.json'
import tr from './tr/common.json'

void i18n.use(initReactI18next).init({
  resources: {
    en: { common: en },
    tr: { common: tr },
  },
  lng: 'en',
  fallbackLng: 'en',
  defaultNS: 'common',
  interpolation: {
    escapeValue: false,
  },
})

export default i18n
