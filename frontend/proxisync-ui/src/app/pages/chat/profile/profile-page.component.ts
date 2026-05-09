import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { UserApiService } from '../../../core/api/user-api.service';
import { Router } from '@angular/router';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-profile',
  templateUrl: './profile-page.component.html',
  imports: [CommonModule, FormsModule]
})
export class ProfileComponent implements OnInit {
  // UI States
  isEditing = false;
  showDeleteConfirm = false;
  loading = false;

  // Data
  user: any = null;

  // Temp data for editing
  editData: any = {};
  imagePreview: string | null = null;
  selectedFile: File | null = null;
constructor(private userService: UserApiService, private router: Router) {}
  ngOnInit() {
    this.loadUserProfile();
    this.resetEditData();
  }

  loadUserProfile() {
    this.userService.getMyProfileDetails().subscribe({
      next: (res) => {
        this.user = res;
        this.resetEditData();
      },
      error: (err) => console.error('Failed to load user', err)
    });
  }

  toggleEdit() {
    this.isEditing = !this.isEditing;
    if (!this.isEditing) {
      this.resetEditData();
      this.selectedFile = null;
    }
  }

  resetEditData() {
    this.editData = { 
      displayName: this.user?.displayName,
      bio: this.user?.bio 
    };
    this.imagePreview = null;
  }

  onFileSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
      this.selectedFile = file;
      const reader = new FileReader();
      reader.onload = () => this.imagePreview = reader.result as string;
      reader.readAsDataURL(file);
    }
  }

  getProfilePic(){
    if(this.imagePreview){
        return this.imagePreview;
    }
    return environment.gatewayUrl + (this.user.profilePictureUrl || 'assets/default-avatar.png')
  }
  onSave() {
    const formData = new FormData();
    formData.append('displayName', this.editData.displayName);
    formData.append('bio', this.editData.bio || '');
    
    if (this.selectedFile) {
      formData.append('profileImage', this.selectedFile);
    }

    this.userService.updateMyProfileDetails(formData).subscribe({
      next: (res: any) => {
        this.user = res.data; // Assuming your API returns the updated user object
        this.isEditing = false;
        this.loading = false;
        this.selectedFile = null;
      },
      error: (err) => {
        console.error('Update failed', err);
        this.loading = false;
        alert("Failed to update: Check console for details.");
      }
    });
  }

  onDeleteAccount() {
    this.userService.deleteMyProfile().subscribe({
      next: () => {
        localStorage.clear(); // Clear tokens/session
        this.router.navigate(['/login']);
      },
      error: (err) => console.error('Delete failed', err)
    });
  }
}