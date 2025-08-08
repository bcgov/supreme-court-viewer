import { Action, Module, Mutation, VuexModule } from 'vuex-module-decorators';

@Module({
  namespaced: true
})
class CivilFileInformation extends VuexModule {
  public civilFileInformation = { }
  public civilFileInfoLoaded = false;
  public hasNonParty = false;

  public showSections = {
    'Case Details': true,
    'Future Appearances': false,
    'Past Appearances': false,
    'All Documents': false,
    'Documents': false,
    'Provided Documents': false,
    'Orders': false
  }

  public civilAppearanceInfo = {}

  @Mutation
  public setCivilFile(civilFileInformation): void {
    this.civilFileInformation = civilFileInformation
  }

  @Action
  public UpdateCivilFile(newCivilFileInformation): void {
    this.context.commit('setCivilFile', newCivilFileInformation)
  }

  @Mutation
  public setCivilFileInfoLoaded(civilFileInfoLoaded): void {
    this.civilFileInfoLoaded = civilFileInfoLoaded
  }

  @Action
  public UpdateCivilFileInfoLoaded(newCivilFileInfoLoaded): void {
    this.context.commit('setCivilFileInfoLoaded', newCivilFileInfoLoaded)
  }

  @Mutation
  public setHasNonParty(hasNonParty: boolean): void {
    this.hasNonParty = hasNonParty
  }

  @Action
  public UpdateHasNonParty(newHasNonParty: boolean): void {
    this.context.commit('setHasNonParty', newHasNonParty)
  }

  @Mutation
  public setShowSections(showSections): void {
    this.showSections = showSections
  }

  @Action
  public UpdateShowSections(newShowSections): void {
    this.context.commit('setShowSections', newShowSections)
  }

  @Mutation
  public setCivilAppearanceInfo(civilAppearanceInfo): void {
    this.civilAppearanceInfo = civilAppearanceInfo
  }

  @Action
  public UpdateCivilAppearanceInfo(newCivilAppearanceInfo): void {
    this.context.commit('setCivilAppearanceInfo', newCivilAppearanceInfo)
  }

}
export default CivilFileInformation 