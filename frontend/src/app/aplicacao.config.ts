import { ApplicationConfig, provideZonelessChangeDetection } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';

export const configuracaoAplicacao: ApplicationConfig = {
  providers: [provideZonelessChangeDetection(), provideHttpClient()],
};
