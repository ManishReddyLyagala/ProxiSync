import { Component } from "@angular/core";
import { AuthApiService } from "../../../core/api/auth-api.service";
import { Router, RouterLink } from "@angular/router";
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";

@Component({
    selector: 'app-register-page',
    standalone: true,
    templateUrl: './register-page.component.html',
    imports: [CommonModule, FormsModule, RouterLink]
})
export class RegisterPageComponent{
    registerData = {
    username: '',
    email: '',
    password: '',
    displayName: ''
  };
  
  selectedFile: File | null = null;
  imagePreview: string | null = null;
  
  loading = false;
  error = '';
    // username = ''; email= ''; password=''; displayName='';
    // file?: File; loading=false; error='';

    constructor(private api: AuthApiService, private router: Router){}
    
    onFileSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
      this.selectedFile = file;
      const reader = new FileReader();
      reader.onload = () => this.imagePreview = reader.result as string;
      reader.readAsDataURL(file);
    }
  }
    submit() {
    this.error = '';
    
    if (!this.registerData.username || !this.registerData.email || !this.registerData.password) {
      this.error = 'Please fill in all required fields';
      return;
    }

    this.loading = true;

    // Use FormData for multipart/form-data
    const formData = new FormData();
    formData.append('username', this.registerData.username);
    formData.append('email', this.registerData.email);
    formData.append('password', this.registerData.password);
    formData.append('displayName', this.registerData.displayName);
    
    if (this.selectedFile) {
      formData.append('profileImage', this.selectedFile, this.selectedFile.name);
    }

    this.api.register(formData).subscribe({
      next: (res: any) => {
        if (res.success) {
          this.router.navigate(['/login']); // Redirect to login after success
        }
        this.loading = false;
      },
      error: e => {
        this.error = e?.error?.message || 'Registration failed';
        this.loading = false;
      }
    });
}
}