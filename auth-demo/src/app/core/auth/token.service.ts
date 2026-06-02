import { Injectable } from '@angular/core';
import { UserInfo } from '../models/auth.model';

/**
 * CHIẾN LƯỢC LƯU TOKEN AN TOÀN:
 *
 * Access Token → Lưu trong biến JavaScript (in-memory)
 *   - XSS không đọc được vì không có trong localStorage/sessionStorage
 *   - Mất khi F5 → Angular tự dùng refresh token để lấy lại
 *
 * Refresh Token → Backend set vào HttpOnly Cookie
 *   - JavaScript KHÔNG ĐỌC ĐƯỢC (HttpOnly)
 *   - Trình duyệt tự gửi kèm request (withCredentials: true)
 *   - SameSite=Strict chặn CSRF
 */
@Injectable({ providedIn: 'root' })
export class TokenService {
  // Token chỉ sống trong JS heap — mất khi F5, reload, đóng tab
  private accessToken: string | null = null;
  private tokenExpiry: Date | null = null;
  private userInfo: UserInfo | null = null;

  setAccessToken(token: string, expiresIn: number): void {
    this.accessToken = token;
    // Trừ 60 giây để refresh sớm trước khi hết hạn
    this.tokenExpiry = new Date(Date.now() + (expiresIn - 60) * 1000);
  }

  getAccessToken(): string | null {
    if (!this.accessToken || !this.tokenExpiry) return null;
    if (new Date() >= this.tokenExpiry) {
      this.clearAll();
      return null;
    }
    return this.accessToken;
  }

  isValid(): boolean {
    return this.getAccessToken() !== null;
  }

  setUserInfo(user: UserInfo): void {
    this.userInfo = user;
  }

  getUserInfo(): UserInfo | null {
    return this.userInfo;
  }

  clearAll(): void {
    this.accessToken = null;
    this.tokenExpiry = null;
    this.userInfo = null;
  }
}
