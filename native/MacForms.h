#ifndef MAC_FORMS_H
#define MAC_FORMS_H

#ifdef __cplusplus
extern "C" {
#endif

/* Call these functions on the application's main thread. Each create owns one
   handle until destroy. Copy returned UTF-8 strings before the next read. */
typedef void (*mf_callback)(void *handle, int event);
enum mf_kind {
    MF_FORM = 1, MF_PANEL, MF_LABEL, MF_BUTTON, MF_TEXT_BOX, MF_CHECK_BOX,
    MF_COMBO_BOX, MF_LIST_BOX, MF_MULTILINE, MF_RADIO_BUTTON, MF_GROUP_BOX,
    MF_PASSWORD, MF_TABS, MF_TAB_PAGE, MF_DATE, MF_NUMBER, MF_GRID, MF_MENU_ITEM, MF_MENU_SEPARATOR,
    MF_PROGRESS, MF_TRACK, MF_TREE, MF_EDITABLE_COMBO, MF_LIST_VIEW = 24, MF_CHECKED_LIST = 25, MF_RICH_TEXT = 26, MF_MASKED_TEXT = 27, MF_DOMAIN = 33, MF_LINK_LABEL = 34, MF_SPLITTER = 35
};
enum mf_event {
    MF_CLICK = 1, MF_TEXT_CHANGED, MF_CHECKED_CHANGED, MF_SELECTION_CHANGED,
    MF_CLOSED, MF_RESIZED, MF_VALUE_CHANGED
};

void mf_init(void);
void mf_run(void);
void mf_stop(void);
void mf_pump(void);
void *mf_create(int kind, mf_callback callback, const char *text);
void mf_destroy(void *handle);
void mf_parent(void *handle, void *parent);
void mf_frame(void *handle, int x, int y, int width, int height);
int mf_width(void *handle);
int mf_height(void *handle);
void mf_text(void *handle, const char *text);
const char *mf_get_text(void *handle);
void mf_visible(void *handle, int visible);
void mf_readonly(void *handle,int readOnly);
void mf_enabled(void *handle, int enabled);
void mf_checked(void *handle, int value);
int mf_get_checked(void *handle);
void mf_add_item(void *handle, const char *text);
void mf_clear_items(void *handle);
void mf_select(void *handle, int index);
int mf_get_selected(void *handle);
int mf_focus(void *handle);
void mf_center(void *handle);
void mf_default_button(void *form, void *button);
void mf_tooltip(void *handle, const char *text);
void mf_post(void (*callback)(void)); /* May be called from any thread. */
void mf_date_value(void *handle, long long unix_seconds);
long long mf_get_date_value(void *handle);
void mf_number(void *handle, double value, double minimum, double maximum);
double mf_get_number(void *handle);
void mf_style(void *handle, const char *font_name, float points, int bold, int argb, int align);
int mf_alert(void *ownerView,const char *text,const char *title,int buttons,int icon,int defaultButton,int options);
/* Show an Open (0), Save (1), or Folder (2) panel. The caller owns the JSON response. */
char *mf_pick_file(void *owner_view, int kind, const char *json_options);
void mf_free(void *data);
void mf_grid_columns(void *handle, const char *json);
void mf_grid_header_height(void *handle, int height);
void mf_grid_rows(void *handle, const char *json);
void mf_progress(void *handle, int min, int max, int value, int marquee);
void mf_track(void *handle, int min, int max, int value, int tick_frequency, int tick_style, int vertical);
int mf_track_value(void *handle);
void mf_tree_nodes(void *handle, const char *json);
void mf_tree_select(void *handle, int id);
int mf_tree_selected(void *handle);
void mf_tree_expand(void *handle, int id, int expand, int recursive);
int mf_tree_expanded(void *handle, int id);

#ifdef __cplusplus
}
#endif
#endif
