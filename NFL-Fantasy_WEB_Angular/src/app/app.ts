// src/app/app.ts - REPLACE existing content
import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Auth } from './core/services/auth';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: '<router-outlet></router-outlet>',
  styles: []
})
export class App implements OnInit {
  private auth = inject(Auth);

  ngOnInit() {
    // Initialize authentication state on app load
    // The auth service will handle token validation
  }
}